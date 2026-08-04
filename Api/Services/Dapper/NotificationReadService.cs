using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class NotificationReadService(IDbConnection db) : INotificationReadService
{
    public async Task<IReadOnlyDictionary<string, int>> GetMyRequestCountsByTypeAsync(Guid userId)
    {
        // 9 個申請類型分別由不同父表表達；UNION ALL 一次撈所有 type 的件數，
        // 篩選條件：申請人 = 當前使用者 AND ApprovalStatus IN ('pending', 'returned')
        // - payment_request / advance / write_off / travel_write_off：SubmittedById
        // - leave / travel / holiday_travel / overtime / travel_payment：EmployeeId
        // - travel vs holiday_travel：共用 TravelRequests 表，靠 IsHolidayTravel 區分
        const string sql = """
            SELECT 'payment_request' AS Type, COUNT(*) AS Cnt
              FROM PaymentRequests
              WHERE SubmittedById = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'leave', COUNT(*)
              FROM LeaveRequests
              WHERE EmployeeId = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'travel', COUNT(*)
              FROM TravelRequests
              WHERE EmployeeId = @UserId
                AND IsHolidayTravel = 0
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'holiday_travel', COUNT(*)
              FROM TravelRequests
              WHERE EmployeeId = @UserId
                AND IsHolidayTravel = 1
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'overtime', COUNT(*)
              FROM OvertimeRequests
              WHERE EmployeeId = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'advance', COUNT(*)
              FROM AdvanceRequests
              WHERE SubmittedById = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'write_off', COUNT(*)
              FROM WriteOffRecords
              WHERE SubmittedById = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'travel_write_off', COUNT(*)
              FROM TravelWriteOffRecords
              WHERE SubmittedById = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'travel_payment', COUNT(*)
              FROM TravelPaymentRequests
              WHERE EmployeeId = @UserId
                AND ApprovalStatus IN ('pending', 'returned')
            UNION ALL
            SELECT 'leave_revocation', COUNT(*)
              FROM LeaveRevocations
              WHERE EmployeeId = @UserId
                AND ApprovalStatus IN ('pending', 'returned');
            """;

        var rows = await db.QueryAsync<(string Type, int Cnt)>(sql, new { UserId = userId });
        return rows.ToDictionary(r => r.Type, r => r.Cnt);
    }

    public async Task<IReadOnlyList<RecentApprovalDto>> GetRecentApprovedMyRequestsAsync(Guid userId, DateTime since)
    {
        // 我送出且狀態為 approved 的 9 種父表（申請人欄位對應同 GetMyRequestCountsByTypeAsync），
        // JOIN 多型 ApprovalRecords 取核准動作時間；只取 @Since 之後仍有核准動作者（限制掃描列），
        // 每張單以 MAX(ReviewedAt) 作為最終核准時間 ApprovedAt。
        // travel vs holiday_travel：共用 TravelRequests，靠 IsHolidayTravel 區分；
        // ApprovalRecords.ApplicationType 對 holiday_travel 亦存 'holiday_travel'，故 JOIN 可直接對應。
        const string sql = """
            WITH MyApproved AS (
                SELECT 'payment_request' AS Type, Id FROM PaymentRequests
                  WHERE SubmittedById = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'leave', Id FROM LeaveRequests
                  WHERE EmployeeId = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'travel', Id FROM TravelRequests
                  WHERE EmployeeId = @UserId AND IsHolidayTravel = 0 AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'holiday_travel', Id FROM TravelRequests
                  WHERE EmployeeId = @UserId AND IsHolidayTravel = 1 AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'overtime', Id FROM OvertimeRequests
                  WHERE EmployeeId = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'advance', Id FROM AdvanceRequests
                  WHERE SubmittedById = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'write_off', Id FROM WriteOffRecords
                  WHERE SubmittedById = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'travel_write_off', Id FROM TravelWriteOffRecords
                  WHERE SubmittedById = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'travel_payment', Id FROM TravelPaymentRequests
                  WHERE EmployeeId = @UserId AND ApprovalStatus = 'approved'
                UNION ALL
                SELECT 'leave_revocation', Id FROM LeaveRevocations
                  WHERE EmployeeId = @UserId AND ApprovalStatus = 'approved'
            )
            SELECT m.Type AS Type, m.Id AS Id, MAX(ar.ReviewedAt) AS ApprovedAt
              FROM MyApproved m
              JOIN ApprovalRecords ar
                ON ar.ApplicationType = m.Type
               AND ar.ApplicationId   = m.Id
               AND ar.Action          = 'approved'
               AND ar.ReviewedAt      > @Since
              GROUP BY m.Type, m.Id;
            """;

        var rows = await db.QueryAsync<RecentApprovalDto>(sql, new { UserId = userId, Since = since });
        return rows.ToList();
    }
}
