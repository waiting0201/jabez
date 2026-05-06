using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 簽核升級服務實作。
/// 當申請人即審核者（自審）時，根據申請類型往上層部門尋找合適的審核者。
///
/// 加班：上層主管 → 主管請假？找代理人 → 沒有？繼續往上 → 停在總監前
/// 請假：上層主管 → 沒有？繼續往上 → 停在總監前
/// 出差：上層主管 → 沒有？繼續往上 → 無上限（可到總監）
/// </summary>
public sealed class EscalationService(AppDbContext db) : IEscalationService
{
    /// <summary>總監職稱 ID</summary>
    private const int DirectorJobTitleId = 5;

    /// <summary>遞迴深度上限，防止資料錯誤導致無限迴圈</summary>
    private const int MaxDepth = 10;

    public async Task<EscalationResult?> TryEscalateAsync(
        ApprovalStep step, User applicant, string applicationType,
        IReadOnlySet<Guid>? excludeUserIds = null)
    {
        // 只處理「使用申請人部門」的步驟（動態匹配）
        if (!step.UseApplicantDepartment)
            return null;

        // 判斷申請人是否為該步驟的審核者（自審）
        if (!IsApplicantTheReviewer(step, applicant))
            return null;

        // 申請人沒有部門 → 無法升級
        if (applicant.DepartmentId is null)
            throw AppException.BadRequest("找不到可審核的主管，無法送出申請。（申請人未設定部門）");

        // 載入部門階層
        var dept = await db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == applicant.DepartmentId);

        if (dept is null)
            throw AppException.BadRequest("找不到可審核的主管，無法送出申請。（部門不存在）");

        // 是否停在總監之前
        bool stopBeforeDirector = applicationType is "leave" or "overtime";
        // 加班才檢查代理人
        bool checkDelegate = applicationType is "overtime";

        var visited = new HashSet<int> { dept.Id }; // 防止部門循環

        var currentDept = dept;
        for (int depth = 0; depth < MaxDepth; depth++)
        {
            if (currentDept.ParentId is null)
                break; // 已到頂層

            var parentDept = await db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == currentDept.ParentId);

            if (parentDept is null)
                break;

            // 防止部門循環
            if (!visited.Add(parentDept.Id))
                break;

            // 找該部門中符合步驟職稱條件的主管
            var manager = await FindManagerInDepartmentAsync(
                parentDept.Id, step.JobTitleId, applicant.Id, stopBeforeDirector, excludeUserIds);

            if (manager is not null)
            {
                if (checkDelegate)
                {
                    // 加班：檢查該主管是否請假中
                    bool onLeave = await IsOnLeaveAsync(manager.Id);
                    if (onLeave)
                    {
                        // 找代理人（同樣排除已在歷史中審過的人）
                        if (manager.AgentUserId is not null
                            && manager.AgentUserId != applicant.Id
                            && (excludeUserIds is null || !excludeUserIds.Contains(manager.AgentUserId.Value)))
                        {
                            var agent = await db.Users.AsNoTracking()
                                .FirstOrDefaultAsync(u => u.Id == manager.AgentUserId
                                    && u.Status == "active");

                            if (agent is not null)
                            {
                                return new EscalationResult(
                                    agent.Id,
                                    manager.Id,  // 代理誰
                                    true);
                            }
                        }
                        // 代理人也沒有 → 繼續往上找
                        currentDept = parentDept;
                        continue;
                    }
                }

                // 主管可用（未請假或非加班類型）
                return new EscalationResult(manager.Id, null, true);
            }

            currentDept = parentDept;
        }

        throw AppException.BadRequest("找不到可審核的主管，無法送出申請。");
    }

    /// <summary>判斷申請人是否符合此步驟的審核者條件（即「自己審自己」）</summary>
    private static bool IsApplicantTheReviewer(ApprovalStep step, User applicant)
    {
        bool jobTitleMatch = step.JobTitleId is null || step.JobTitleId == applicant.JobTitleId;

        // UseApplicantDepartment = true 時，申請人自己當然在自己的部門
        return jobTitleMatch;
    }

    /// <summary>在指定部門中尋找符合職稱的主管（會排除已在歷史中審過此申請者）</summary>
    private async Task<User?> FindManagerInDepartmentAsync(
        int departmentId, int? requiredJobTitleId, Guid excludeUserId, bool stopBeforeDirector,
        IReadOnlySet<Guid>? excludeUserIds = null)
    {
        var query = db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == departmentId
                && u.Status == "active"
                && u.Id != excludeUserId
                && !u.IsSuperAdmin);

        if (requiredJobTitleId is not null)
            query = query.Where(u => u.JobTitleId == requiredJobTitleId);

        if (stopBeforeDirector)
            query = query.Where(u => u.JobTitleId != DirectorJobTitleId);

        // 排除已在歷史中審過此申請者
        if (excludeUserIds is not null && excludeUserIds.Count > 0)
        {
            var excludeIds = excludeUserIds.ToArray();
            query = query.Where(u => !excludeIds.Contains(u.Id));
        }

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>檢查使用者今天是否請假中（有已核准且涵蓋今天的假單）</summary>
    private async Task<bool> IsOnLeaveAsync(Guid userId)
    {
        var today = Clock.Today;
        return await db.LeaveRequests.AnyAsync(lr =>
            lr.EmployeeId == userId
            && lr.ApprovalStatus == "approved"
            && lr.StartDate.Date <= today
            && lr.EndDate.Date >= today);
    }
}
