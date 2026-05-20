using Dapper;
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
                AND ApprovalStatus IN ('pending', 'returned');
            """;

        var rows = await db.QueryAsync<(string Type, int Cnt)>(sql, new { UserId = userId });
        return rows.ToDictionary(r => r.Type, r => r.Cnt);
    }
}
