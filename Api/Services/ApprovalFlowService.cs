using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 簽核流程輔助服務：送出申請時處理「申請人即審核者」的步驟。
/// 對 payment_request 維持原有自動跳過邏輯；
/// 對 leave / travel / overtime 改為呼叫 EscalationService 升級審核。
/// </summary>
public sealed class ApprovalFlowService(
    AppDbContext db,
    IEscalationService escalationService) : IApprovalFlowService
{
    public async Task<int?> ResolveApprovalItemIdAsync(string applicationType, int? applicantDepartmentId)
    {
        // 部門階層繼承：優先序＝申請人部門 > 最近祖先部門（沿 ParentId 逐層往上）> 通用預設(null)。
        // 子部門未設專屬流程時，會自動沿用最近一層有設定流程的上層部門。
        var chain = await BuildDepartmentChainAsync(applicantDepartmentId);

        var candidates = await db.ApprovalItems
            .AsNoTracking()
            .Where(ai => ai.ApplicationType == applicationType && ai.IsActive
                && (ai.DepartmentId == null || chain.Contains(ai.DepartmentId.Value)))
            .Select(ai => new { ai.Id, ai.DepartmentId })
            .ToListAsync();

        if (candidates.Count == 0)
            return null;

        // 距離越近越優先：自身=0、父=1…；通用預設(null)排最後。
        return candidates
            .OrderBy(c => c.DepartmentId is null ? int.MaxValue : chain.IndexOf(c.DepartmentId.Value))
            .Select(c => (int?)c.Id)
            .First();
    }

    /// <summary>
    /// 建立部門階層鏈：由指定部門逐層沿 ParentId 往上，回傳 [自身, 父, 祖父, …]（順序＝由近到遠）。
    /// departmentId 為 null 時回傳空清單。內含 visited 防止部門循環。
    /// </summary>
    private async Task<List<int>> BuildDepartmentChainAsync(int? departmentId)
    {
        var chain = new List<int>();
        if (departmentId is null)
            return chain;

        // 部門表筆數少，一次載入 (Id, ParentId) 在記憶體往上走，避免逐層 round-trip。
        var parentMap = await db.Departments
            .AsNoTracking()
            .Select(d => new { d.Id, d.ParentId })
            .ToDictionaryAsync(d => d.Id, d => d.ParentId);

        int? current = departmentId;
        while (current is not null && !chain.Contains(current.Value))
        {
            chain.Add(current.Value);
            current = parentMap.TryGetValue(current.Value, out var parent) ? parent : null;
        }
        return chain;
    }

    public async Task<(int startStep, bool autoApproved, EscalationResult? escalation)>
        ResolveStartingStepAsync(int? approvalItemId, Guid applicantId, string applicationType,
            IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers = null,
            decimal? requestDays = null)
    {
        // 註：初次送出無 approved 紀錄，毋需歷史比對，保持既有「自審 + 升級」邏輯。
        if (approvalItemId is null)
            return (1, false, null);

        var steps = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        if (steps.Count == 0)
            return (1, false, null);

        var applicant = await db.Users.AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == applicantId);
        if (applicant is null)
            return (1, false, null);

        // 送單後的有效指定步驟＝原生 UseApplicantDesignated ∪ 有 designee 綁定者（例外命中的快照）。
        // designee 已由 DesignatedReviewerHelper.ValidateAndNormalizeAsync 剔除非法綁定。
        var designatedSet = DesignatedReviewerHelper.EffectiveDesignatedStepOrders(steps, designatedReviewers);

        // 被抑制的指定步驟（首個指定步驟＝所選部門最高職稱 → 其後指定步驟自動跳過）
        var suppressed = designatedReviewers is { Count: > 0 }
            ? await DesignatedReviewerHelper.GetSuppressedDesignatedStepOrdersAsync(db, approvalItemId.Value, designatedReviewers, designatedSet)
            : new HashSet<int>();

        // 送單前防呆：固定審核者池的步驟若查無可審人員，直接擋下送出。
        // 否則單子送得出去卻卡在半路 —— 該關沒有人通得過 AuthorizeStepAsync，也不會有人收到通知，只能靠 Superadmin 介入。
        await ValidateFixedStepsHaveReviewersAsync(steps, applicant, applicationType, designatedSet, requestDays);

        int currentStep = 1;
        int directSupervisorRank = 0; // 追蹤目前是第幾個上層級步驟（0-based）
        foreach (var step in steps)
        {
            // ── 天數門檻（目前僅請假傳 requestDays）：MinDays > 申請天數 → 乾淨跳過此步驟 ──
            // 以 currentStep++ 跳過（與其他略過模式一致），維持 currentStep 與 StepOrder 對齊。
            if (StepBelowMinDays(step, requestDays))
            {
                currentStep++;
                continue;
            }

            // ── 指定審核模式（原生 UseApplicantDesignated 或例外命中）：審核者是申請人指定的人 ──
            if (designatedSet.Contains(step.StepOrder))
            {
                // 被抑制 → 乾淨跳過（不進 firstReviewer==null 的 throw 分支）
                if (suppressed.Contains(step.StepOrder))
                {
                    currentStep++;
                    continue;
                }
                // 僅取「屬於本步驟」的指定審核者（以 ApprovalStepOrder 綁定），再取 min StepOrder 的第 1 位判斷
                var stepReviewers = designatedReviewers?
                    .Where(r => r.ApprovalStepOrder == step.StepOrder)
                    .OrderBy(r => r.StepOrder)
                    .ToList();
                var firstReviewer = stepReviewers?.FirstOrDefault();

                // 自審規則分兩組：
                //   Group A 全程禁止自審（任一位置為申請人即報錯）：leave / travel / overtime / travel_payment
                //   Group B 首位跳過（申請人排第 1 位 → 自動跳過此步驟；2+ 位置不檢查）：payment_request / advance / write_off / travel_write_off / holiday_travel
                // 此處先處理 Group A：當 applicationType 不在 Group B 名單內 → 套用 Group A 規則
                if (applicationType is not ("payment_request" or "advance" or "write_off" or "travel_write_off" or "holiday_travel" or "pre_review"))
                {
                    bool anyIsSelf = stepReviewers?.Any(r => r.ReviewerId == applicantId) ?? false;
                    if (anyIsSelf)
                        throw AppException.BadRequest("指定審核者不能是申請人本人。");
                }

                if (firstReviewer is not null && firstReviewer.ReviewerId != applicantId)
                {
                    // 有指定審核者且第 1 位不是自己 → 從這步開始
                    return (currentStep, false, null);
                }
                else if (firstReviewer is not null && firstReviewer.ReviewerId == applicantId)
                {
                    // payment_request / advance 自審第 1 位 → 跳過此步驟
                    currentStep++;
                    continue;
                }
                else
                {
                    // designatedReviewers 為 null 或空：理論上 Handler 層守門會先擋下，
                    // 此處作 defense-in-depth — 流程明確要求指定審核者卻沒提供，視為呼叫端 bug。
                    // 與 8 個 SubmitAsync Handler 的守門訊息保持一致。
                    throw AppException.BadRequest("此簽核流程包含申請人指定審核步驟，請提供指定審核者。");
                }
            }

            // ── UseDirectSupervisor 模式：找同部門中第 N 層上級 ──
            if (step.UseDirectSupervisor)
            {
                var targetLevel = await FindNthSuperiorLevelAsync(applicant, directSupervisorRank);
                directSupervisorRank++;
                if (targetLevel.HasValue)
                    return (currentStep, false, null); // 有上層級 → 從這步開始

                // 同部門找不到更高階者（例：申請人已是部門最高階）→ 沿部門 ParentId 往上層部門找。
                // 找到 → 以升級審核指派該員（寫 EscalationOverride 由呼叫端負責）。
                // 帶入後續固定關卡範圍：流程後面本來就會簽到的人不提前拉進這一關（見 StepReviewerScope）。
                var superiorId = await escalationService.FindSuperiorInAncestorDepartmentsAsync(
                    applicant, null,
                    BuildLaterFixedStepScopes(steps, step.StepOrder, applicant, designatedSet, requestDays));
                if (superiorId.HasValue)
                    return (currentStep, false, new EscalationResult(superiorId.Value, null, true));

                // 連上層部門都找不到 → 維持既有行為：跳過此步驟
                currentStep++;
                continue;
            }

            bool isSelfReview = IsApplicantTheReviewer(step, applicant);

            if (!isSelfReview)
            {
                // 請款/預支/沖銷：若步驟使用申請人部門但該部門無符合條件的審核者，也跳過
                if (SkipEmptyApplicantDeptTypes.Contains(applicationType) && step.UseApplicantDepartment && applicant.DepartmentId.HasValue)
                {
                    bool hasReviewer = await db.Users.AsNoTracking().AnyAsync(u =>
                        u.DepartmentId == applicant.DepartmentId
                        && u.Id != applicantId
                        && !u.IsSuperAdmin
                        && (step.JobTitleId == null || u.JobTitleId == step.JobTitleId));

                    if (!hasReviewer)
                    {
                        // 該部門找不到審核者 → 跳過此步驟
                        currentStep++;
                        continue;
                    }
                }

                return (currentStep, false, null); // 這一步不是自己且有審核者 → 從這步開始
            }

            // 自審情境：根據申請類型決定處理方式
            // 銷假沿用請假的升級規則（Group A 全程禁止自審由上方否定清單自動涵蓋）
            if (applicationType is "leave" or "leave_revocation" or "travel" or "overtime")
            {
                // 嘗試升級審核（會拋出 AppException 如果找不到人）
                var escalation = await escalationService.TryEscalateAsync(step, applicant, applicationType);
                if (escalation is not null)
                    return (currentStep, false, escalation);
            }

            // payment_request 或 TryEscalate 回傳 null → 維持原有自動跳過邏輯
            currentStep++;
        }

        // 所有步驟都被跳過 → 自動核准
        return (steps.Count, true, null);
    }

    /// <summary>
    /// 「UseApplicantDepartment 步驟在申請人部門找不到審核者時，明確跳過該關」的申請類型（請款類）。
    /// 其餘類型遇到同樣情況會被 ValidateFixedStepsHaveReviewersAsync 擋下送出。
    /// </summary>
    private static readonly string[] SkipEmptyApplicantDeptTypes =
        ["payment_request", "advance", "write_off", "travel_write_off", "pre_review"];

    /// <summary>
    /// 送單前防呆：檢查流程中每個「固定審核者池」的步驟至少有一位可審的人，否則丟 400 擋下送出。
    ///
    /// 固定池步驟（綁部門 / 職稱，含 UseApplicantDepartment）與上層級步驟不同 —— 引擎不會因為查無人員而跳過，
    /// 而是照樣停在該關；但沒有人通得過 AuthorizeStepAsync 的部門 / 職稱比對，也沒有人會收到通知，
    /// 結果是單子卡死在半路，只能靠 Superadmin 介入。與其讓它送出後才卡住，不如送單當下就講清楚。
    ///
    /// 以下步驟**不檢查**（各自另有合法的「無人可審」出路）：
    ///   · MinDays 門檻擋掉的步驟  —— 這張單根本不走這關
    ///   · 指定審核步驟            —— 由 DesignatedReviewerHelper.ValidateAndNormalizeAsync 負責
    ///   · UseDirectSupervisor     —— 找不到人時往上層部門升級，再找不到則設計上允許跳過
    ///   · 申請人即審核者（自審）  —— 另有跳過（請款類）／升級（請假・出差・加班）機制
    ///   · 請款類的 UseApplicantDepartment —— 現行明確設計為「該部門無人則跳過」
    /// </summary>
    private async Task ValidateFixedStepsHaveReviewersAsync(
        List<Models.Entities.ApprovalStep> steps,
        Models.Entities.User applicant,
        string applicationType,
        IReadOnlySet<int> designatedStepOrders,
        decimal? requestDays)
    {
        foreach (var step in steps)
        {
            if (StepBelowMinDays(step, requestDays))                continue;
            if (designatedStepOrders.Contains(step.StepOrder))      continue;
            if (step.UseDirectSupervisor)                           continue;
            if (IsApplicantTheReviewer(step, applicant))            continue;
            if (step.UseApplicantDepartment
                && SkipEmptyApplicantDeptTypes.Contains(applicationType)) continue;

            if (await HasStepReviewerAsync(step, applicant)) continue;

            throw AppException.BadRequest(await BuildMissingReviewerMessageAsync(step, applicant));
        }
    }

    /// <summary>
    /// 該固定池步驟是否至少有一位可審的人（active、非 superadmin、非申請人本人）。
    /// UseApplicantDepartment 但申請人未設部門 → 一律視為無人（該關的部門條件永遠對不上）。
    /// </summary>
    private async Task<bool> HasStepReviewerAsync(
        Models.Entities.ApprovalStep step, Models.Entities.User applicant)
    {
        int? targetDepartmentId = step.UseApplicantDepartment ? applicant.DepartmentId : step.DepartmentId;
        if (step.UseApplicantDepartment && targetDepartmentId is null)
            return false;

        var query = db.Users.AsNoTracking()
            .Where(u => u.Status == "active" && !u.IsSuperAdmin && u.Id != applicant.Id);

        if (targetDepartmentId.HasValue)
            query = query.Where(u => u.DepartmentId == targetDepartmentId.Value);

        if (step.JobTitleId.HasValue)
            query = query.Where(u => u.JobTitleId == step.JobTitleId.Value);

        return await query.AnyAsync();
    }

    /// <summary>組出「這一關找不到人」的錯誤訊息，帶出部門與職稱讓管理員知道要改哪裡。</summary>
    private async Task<string> BuildMissingReviewerMessageAsync(
        Models.Entities.ApprovalStep step, Models.Entities.User applicant)
    {
        int? targetDepartmentId = step.UseApplicantDepartment ? applicant.DepartmentId : step.DepartmentId;

        var deptText = targetDepartmentId.HasValue
            ? await db.Departments.AsNoTracking()
                .Where(d => d.Id == targetDepartmentId.Value).Select(d => d.Name).FirstOrDefaultAsync()
              ?? $"部門 #{targetDepartmentId.Value}"
            : (step.UseApplicantDepartment ? "申請人部門（申請人未設定部門）" : "不限部門");

        var titleText = step.JobTitleId.HasValue
            ? await db.JobTitles.AsNoTracking()
                .Where(j => j.Id == step.JobTitleId.Value).Select(j => j.Name).FirstOrDefaultAsync()
              ?? $"職稱 #{step.JobTitleId.Value}"
            : "不限職稱";

        return $"簽核流程第 {step.StepOrder} 關找不到可審核的人員（{deptText} / {titleText}），無法送出申請。"
             + "請聯絡管理員調整簽核流程設定或該部門人員職稱。";
    }

    /// <summary>
    /// 找同部門中第 N 層上級的 Level 值（0-based：rank=0 最接近，rank=1 再上一層...）。
    /// Level 數字越小 = 層級越高。回傳 null 表示找不到。
    /// </summary>
    private async Task<int?> FindNthSuperiorLevelAsync(Models.Entities.User applicant, int rank)
    {
        if (applicant.DepartmentId is null || applicant.JobTitle is null)
            return null;

        var applicantLevel = applicant.JobTitle.Level;

        // 取得同部門中所有比申請人高的不重複 Level，由近到遠排列（DESC，因為 Level 越小越高）
        var superiorLevels = await db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == applicant.DepartmentId
                && u.Status == "active"
                && !u.IsSuperAdmin
                && u.JobTitle != null
                && u.JobTitle.Level < applicantLevel)
            .Select(u => u.JobTitle!.Level)
            .Distinct()
            .OrderByDescending(l => l) // 最接近的排前面
            .ToListAsync();

        return rank < superiorLevels.Count ? superiorLevels[rank] : null;
    }

    /// <inheritdoc />
    public async Task<HashSet<Guid>> GetApprovedReviewerIdsAsync(
        string applicationType, int applicationId)
    {
        // 追加預支：只看本批次，否則第 1 輪的審核者會讓追加輪所有步驟被自動跳過（未經審核就核准）
        var roundNo = await AdvanceSupplementService.ResolveCurrentRoundAsync(db, applicationType, applicationId);

        // 退回重送 → 歷史清零：以最近一次 Action='returned' 的 ReviewedAt 當分隔線
        var lastReturnedAt = await db.ApprovalRecords.AsNoTracking()
            .Where(r => r.ApplicationType == applicationType
                     && r.ApplicationId == applicationId
                     && r.RoundNo == roundNo
                     && r.Action == "returned")
            .MaxAsync(r => (DateTime?)r.ReviewedAt) ?? DateTime.MinValue;

        var ids = await db.ApprovalRecords.AsNoTracking()
            .Where(r => r.ApplicationType == applicationType
                     && r.ApplicationId == applicationId
                     && r.RoundNo == roundNo
                     && r.Action == "approved"
                     && r.ReviewedById != null
                     && r.ReviewedAt > lastReturnedAt)
            .Select(r => r.ReviewedById!.Value)
            .Distinct()
            .ToListAsync();

        return [.. ids];
    }

    /// <inheritdoc />
    public async Task<HashSet<Guid>> GetApprovedSupervisorIdsAsync(
        string applicationType, int applicationId)
    {
        // 追加預支：只看本批次（同 GetApprovedReviewerIdsAsync）
        var roundNo = await AdvanceSupplementService.ResolveCurrentRoundAsync(db, applicationType, applicationId);

        var lastReturnedAt = await db.ApprovalRecords.AsNoTracking()
            .Where(r => r.ApplicationType == applicationType
                     && r.ApplicationId == applicationId
                     && r.RoundNo == roundNo
                     && r.Action == "returned")
            .MaxAsync(r => (DateTime?)r.ReviewedAt) ?? DateTime.MinValue;

        // 已 approved 的 reviewerId join Users + JobTitles 過濾 Level=1
        var ids = await (from r in db.ApprovalRecords.AsNoTracking()
                         join u in db.Users.AsNoTracking() on r.ReviewedById equals u.Id
                         join j in db.JobTitles.AsNoTracking() on u.JobTitleId equals j.Id
                         where r.ApplicationType == applicationType
                            && r.ApplicationId == applicationId
                            && r.RoundNo == roundNo
                            && r.Action == "approved"
                            && r.ReviewedById != null
                            && r.ReviewedAt > lastReturnedAt
                            && j.Level == 1
                         select r.ReviewedById!.Value)
                        .Distinct()
                        .ToListAsync();

        return [.. ids];
    }

    /// <inheritdoc />
    public async Task<(int nextStep, bool allSkipped, IReadOnlyList<SkippedStepInfo> skippedSteps, EscalationResult? escalation)>
        SkipUnreviewableStepsAsync(int? approvalItemId, Guid applicantId, int fromStepOrder,
            IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers = null,
            IReadOnlySet<Guid>? approvedReviewerIds = null,
            string? applicationType = null,
            int? applicationId = null,
            IReadOnlySet<Guid>? supervisorIds = null,
            int? priorStepOrder = null,
            decimal? requestDays = null)
    {
        var emptySkipped = Array.Empty<SkippedStepInfo>();

        if (approvalItemId is null)
            return (fromStepOrder, false, emptySkipped, null);

        var steps = await db.ApprovalSteps
            .AsNoTracking()
            .Where(s => s.ApprovalItemId == approvalItemId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        // 天數門檻過濾（目前僅請假傳 requestDays）：MinDays > 申請天數的步驟視為不存在。
        // 本方法全程以 StepOrder 為準（非位置計數），故可安全地整批移除被過濾步驟。
        steps = FilterStepsByMinDays(steps, requestDays);

        if (steps.Count == 0)
            return (fromStepOrder, false, emptySkipped, null);

        var applicant = await db.Users.AsNoTracking()
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == applicantId);
        if (applicant is null)
            return (fromStepOrder, false, emptySkipped, null);

        // 送單後的有效指定步驟＝原生 UseApplicantDesignated ∪ 有 designee 綁定者（例外命中的快照）
        var designatedSet = DesignatedReviewerHelper.EffectiveDesignatedStepOrders(steps, designatedReviewers);

        // 被抑制的指定步驟（首個指定步驟＝所選部門最高職稱 → 其後指定步驟自動跳過）
        var suppressed = designatedReviewers is { Count: > 0 }
            ? await DesignatedReviewerHelper.GetSuppressedDesignatedStepOrdersAsync(db, approvalItemId.Value, designatedReviewers, designatedSet)
            : new HashSet<int>();

        // 直接迭代 StepOrder >= fromStepOrder 的步驟（處理稀疏 StepOrder）
        var remainingSteps = steps.Where(s => s.StepOrder >= fromStepOrder).ToList();

        var skipped = new List<SkippedStepInfo>();

        // ── 相鄰判斷準備：以 ordered StepOrder 索引判定，避免稀疏 StepOrder 數值差距誤判 ──
        var orderedStepOrders = steps.Select(s => s.StepOrder).OrderBy(x => x).ToList();
        // adjacencyAnchorStepOrder = 上一個有審核紀錄（含代簽）的 StepOrder；連鎖跳過時會更新
        int? adjacencyAnchorStepOrder = priorStepOrder;

        foreach (var step in remainingSteps)
        {
            // ── 第一階段：判斷該步驟是否「找不到審核者」 ──
            bool hasReviewer;
            // 上層級步驟改由上層部門主管接手時的升級指派（呼叫端負責寫 EscalationOverride）
            EscalationResult? stepEscalation = null;
            if (designatedSet.Contains(step.StepOrder))
            {
                if (suppressed.Contains(step.StepOrder))
                {
                    // 被抑制 → 視為無審核者，走乾淨跳過（不寫代簽）
                    hasReviewer = false;
                }
                else
                {
                    var firstReviewer = designatedReviewers?
                        .Where(r => r.ApprovalStepOrder == step.StepOrder)
                        .OrderBy(r => r.StepOrder)
                        .FirstOrDefault();
                    hasReviewer = firstReviewer is not null && firstReviewer.ReviewerId != applicantId;
                }
            }
            else if (step.UseDirectSupervisor)
            {
                int rank = steps.Count(s => s.UseDirectSupervisor && s.StepOrder < step.StepOrder);
                var targetLevel = await FindNthSuperiorLevelAsync(applicant, rank);
                hasReviewer = targetLevel.HasValue;

                if (!hasReviewer)
                {
                    // 同部門找不到更高階者 → 沿部門 ParentId 往上層部門找（與送出時 ResolveStartingStepAsync 同源）。
                    // 排除總監歷史已審者，避免把已經審過的人再找回來重審。
                    var superiorId = await escalationService
                        .FindSuperiorInAncestorDepartmentsAsync(applicant, supervisorIds,
                            BuildLaterFixedStepScopes(steps, step.StepOrder, applicant, designatedSet, requestDays));
                    if (superiorId.HasValue)
                    {
                        hasReviewer   = true;
                        stepEscalation = new EscalationResult(superiorId.Value, null, true);
                    }
                }
            }
            else
            {
                hasReviewer = true; // 非上層級／非指定模式皆視為有審核者（既有行為）
            }

            if (!hasReviewer)
                continue; // 找不到審核者 → 跳過（不寫代簽，因為池本來就空）

            // ── 第二階段：跨步驟同人去重（相鄰 step 同人 OR 總監） ──
            // 若 approvedReviewerIds 提供，解析池後判斷是否已由同一人行使過同一份權責：
            //   (A) 代簽人為總監（Level=1） → 維持限縮：要求「全池」皆已審
            //   (B) 與上一審核 step 相鄰     → 放寬為「池中任一人」已審即可（2026-09）
            // 放寬理由：固定部門+職稱 / 上層級的池語意是「這個角色任一人可審」，
            // 同一人在相鄰的前一關已行使過同一份權責，不該被要求連簽兩關
            // （例：品牌事業部 Step1 上層級解析＝Step2 固定「部門協理」同一池，部門有兩位協理時永遠湊不齊全池）。
            if (approvedReviewerIds is not null && approvedReviewerIds.Count > 0)
            {
                int directSupervisorRank = steps.Count(s => s.UseDirectSupervisor && s.StepOrder < step.StepOrder);

                // 升級接手的步驟：審核者池就是被指派的那一位（ResolveReviewerPoolAsync 對這種步驟會回空池，
                // 空池不進去重判定，會害「連續兩個上層級關卡都升級到同一人」變成同一人連審兩次）。
                var pool = stepEscalation is not null
                    ? new List<Guid> { stepEscalation.ReviewerId }
                    : await ResolveReviewerPoolAsync(
                        step, applicant, designatedReviewers, directSupervisorRank, designatedSet);

                // 指定審核步驟不套用放寬：池是申請人逐位點名的人，語意為「這些人都要審」，
                // 與 ApprovalTaskHandler.ProcessReviewAsync 的 in-step while 迴圈（逐位推進、遇未審者停下）一致。
                bool isDesignatedStep = designatedSet.Contains(step.StepOrder);
                bool allApproved = pool.Count > 0 && pool.All(id => approvedReviewerIds.Contains(id));
                bool anyApproved = pool.Count > 0 && pool.Any(id => approvedReviewerIds.Contains(id));

                if (isDesignatedStep ? allApproved : anyApproved)
                {
                    // 代簽人必為「池 ∩ 已審者」，多位時取最早審過此申請者
                    var proxyId = await PickEarliestProxyAsync(
                        pool, approvedReviewerIds, applicationType, applicationId);
                    if (proxyId.HasValue)
                    {
                        // 總監分支維持原本的「全池覆蓋」限縮，放寬只作用於相鄰分支
                        bool proxyIsSupervisor = allApproved
                            && supervisorIds is not null && supervisorIds.Contains(proxyId.Value);

                        bool isAdjacent = false;
                        if (adjacencyAnchorStepOrder.HasValue)
                        {
                            int anchorIdx = orderedStepOrders.IndexOf(adjacencyAnchorStepOrder.Value);
                            int curIdx    = orderedStepOrders.IndexOf(step.StepOrder);
                            isAdjacent    = anchorIdx >= 0 && curIdx == anchorIdx + 1;
                        }

                        if (proxyIsSupervisor || isAdjacent)
                        {
                            skipped.Add(new SkippedStepInfo(step.StepOrder, proxyId.Value, designatedSet.Contains(step.StepOrder)));
                            adjacencyAnchorStepOrder = step.StepOrder; // 連鎖：下一 step 仍可能與此相鄰
                            continue;
                        }
                    }
                }
            }

            return (step.StepOrder, false, skipped, stepEscalation); // 停在這步（含「同人已審但非總監且不相鄰 → 要求重審」）
        }

        // 所有剩餘步驟都跳過
        var maxStepOrder = steps.Max(s => s.StepOrder);
        return (maxStepOrder + 1, true, skipped, null);
    }

    /// <summary>
    /// 從「step 池 ∩ 歷史已審者」中取最早審過此申請者作為代簽人。
    /// 多人時按 ApprovalRecords.ReviewedAt 升序取首位（最早審過此申請的池內成員）；
    /// 查無紀錄 / applicationType 缺漏時回退為交集首位（代簽人必定是實際審過的人）。
    ///
    /// 候選一律先收斂成 pool ∩ approvedReviewerIds —— 不可退回 pool[0]：
    /// 當次審核的 ApprovalRecord 還在 ChangeTracker（呼叫端手動補進 approvedReviewerIds），
    /// 這裡以 AsNoTracking 讀 DB 看不到它；池中若還有從沒審過的人（如同部門另一位同職稱主管），
    /// 退回 pool[0] 會把代簽紀錄與 PDF 簽名章掛到錯的人身上。
    /// </summary>
    private async Task<Guid?> PickEarliestProxyAsync(
        IReadOnlyList<Guid> pool,
        IReadOnlySet<Guid> approvedReviewerIds,
        string? applicationType,
        int? applicationId)
    {
        var candidates = pool.Where(approvedReviewerIds.Contains).ToList();
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        if (applicationType is null || !applicationId.HasValue)
            return candidates[0];

        var candidateSet = candidates.ToHashSet();
        var earliest = await db.ApprovalRecords.AsNoTracking()
            .Where(r => r.ApplicationType == applicationType
                     && r.ApplicationId == applicationId.Value
                     && r.Action == "approved"
                     && r.ReviewedById != null
                     && candidateSet.Contains(r.ReviewedById!.Value))
            .OrderBy(r => r.ReviewedAt)
            .Select(r => r.ReviewedById!.Value)
            .FirstOrDefaultAsync();

        return earliest != Guid.Empty ? earliest : candidates[0];
    }

    /// <summary>
    /// 解析該步驟的「審核者池」：
    /// - 指定審核（原生 UseApplicantDesignated 或例外命中）：該 step 中所有 pending designee 的 ReviewerId
    /// - UseDirectSupervisor：同部門 + 第 N 層上級 Level + active + 非 superadmin + 非申請人
    /// - 固定部門+職稱（含 UseApplicantDepartment）：對應部門 + 職稱 + active + 非 superadmin + 非申請人
    /// 回傳 List&lt;Guid&gt;（最多 50 筆防呆）。
    ///
    /// active 條件與送單防呆 HasStepReviewerAsync 一致 —— 離職 / 停用帳號若混進池中，
    /// 會讓仍走「全池皆已審」的總監去重分支永遠不成立。
    /// </summary>
    private async Task<List<Guid>> ResolveReviewerPoolAsync(
        Models.Entities.ApprovalStep step,
        Models.Entities.User applicant,
        IReadOnlyList<DesignatedReviewerRequest>? designatedReviewers,
        int directSupervisorRank,
        IReadOnlySet<int> designatedStepOrders)
    {
        // ── 指定審核：該 step 內所有 designee（以 ApprovalStepOrder 綁定本步驟） ──
        if (designatedStepOrders.Contains(step.StepOrder))
        {
            return designatedReviewers?
                .Where(r => r.ApprovalStepOrder == step.StepOrder && r.ReviewerId != applicant.Id)
                .Select(r => r.ReviewerId)
                .Distinct()
                .Take(50)
                .ToList() ?? [];
        }

        // ── UseDirectSupervisor：找該 rank 對應 Level，再查同部門該 Level 全部使用者 ──
        if (step.UseDirectSupervisor)
        {
            var targetLevel = await FindNthSuperiorLevelAsync(applicant, directSupervisorRank);
            if (!targetLevel.HasValue || applicant.DepartmentId is null) return [];

            return await db.Users.AsNoTracking()
                .Where(u => u.DepartmentId == applicant.DepartmentId
                    && u.Status == "active"
                    && !u.IsSuperAdmin
                    && u.Id != applicant.Id
                    && u.JobTitle != null
                    && u.JobTitle.Level == targetLevel.Value)
                .Select(u => u.Id)
                .Take(50)
                .ToListAsync();
        }

        // ── 固定部門+職稱（含 UseApplicantDepartment） ──
        int? targetDepartmentId = step.UseApplicantDepartment
            ? applicant.DepartmentId
            : step.DepartmentId;
        if (targetDepartmentId is null) return [];

        var query = db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == targetDepartmentId.Value
                && u.Status == "active"
                && !u.IsSuperAdmin
                && u.Id != applicant.Id);

        if (step.JobTitleId.HasValue)
            query = query.Where(u => u.JobTitleId == step.JobTitleId.Value);

        return await query
            .Select(u => u.Id)
            .Take(50)
            .ToListAsync();
    }

    /// <summary>
    /// 天數門檻判定：此步驟是否因「MinDays > 申請天數」而不該納入。
    /// requestDays 為 null（未提供天數，如非請假類型）→ 一律不套用門檻（回 false）。
    /// </summary>
    private static bool StepBelowMinDays(Models.Entities.ApprovalStep step, decimal? requestDays)
        => requestDays.HasValue && step.MinDays.HasValue && requestDays.Value < step.MinDays.Value;

    /// <summary>
    /// 組出「本流程 <paramref name="fromStepOrder"/> 之後的固定關卡審核者範圍」，供上層級關卡的
    /// 升級指派避開「流程後面本來就會簽到的人」（見 <see cref="StepReviewerScope"/>）。
    ///
    /// 只納入**固定池**關卡 —— 這種關卡確定會有人接手，才足以作為「這一關可以跳過」的保證：
    ///   · MinDays 擋掉的步驟   → 這張單根本不走這關
    ///   · 指定審核步驟         → 人由申請人臨時點名，不保證會是同一位
    ///   · 上層級步驟           → 同樣可能無人（該關自身也會走升級），不能當保證
    ///   · 不限部門也不限職稱   → 範圍等於全公司，會把所有候選人都排除掉
    /// </summary>
    private static List<StepReviewerScope> BuildLaterFixedStepScopes(
        List<Models.Entities.ApprovalStep> steps,
        int fromStepOrder,
        Models.Entities.User applicant,
        IReadOnlySet<int> designatedStepOrders,
        decimal? requestDays)
    {
        var scopes = new List<StepReviewerScope>();

        foreach (var s in steps.Where(s => s.StepOrder > fromStepOrder))
        {
            if (StepBelowMinDays(s, requestDays))             continue;
            if (designatedStepOrders.Contains(s.StepOrder))   continue;
            if (s.UseApplicantDesignated || s.UseDirectSupervisor) continue;

            int? departmentId = s.UseApplicantDepartment ? applicant.DepartmentId : s.DepartmentId;
            if (departmentId is null && s.JobTitleId is null) continue;

            scopes.Add(new StepReviewerScope(departmentId, s.JobTitleId));
        }

        return scopes;
    }

    /// <summary>移除所有 MinDays > requestDays 的步驟（乾淨略過，等同該步驟不存在）。</summary>
    private static List<Models.Entities.ApprovalStep> FilterStepsByMinDays(
        List<Models.Entities.ApprovalStep> steps, decimal? requestDays)
        => requestDays.HasValue
            ? steps.Where(s => !StepBelowMinDays(s, requestDays)).ToList()
            : steps;

    /// <summary>判斷申請人是否符合此步驟的審核者條件（即「自己審自己」）</summary>
    private static bool IsApplicantTheReviewer(Models.Entities.ApprovalStep step, Models.Entities.User applicant)
    {
        bool jobTitleMatch = step.JobTitleId is null || step.JobTitleId == applicant.JobTitleId;

        bool deptMatch;
        if (step.UseApplicantDepartment)
        {
            deptMatch = true;
        }
        else
        {
            deptMatch = step.DepartmentId is null || step.DepartmentId == applicant.DepartmentId;
        }

        return jobTitleMatch && deptMatch;
    }
}
