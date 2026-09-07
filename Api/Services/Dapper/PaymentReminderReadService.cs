using Dapper;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 撥款提醒：UNION 5 種申請類型的 installments，撈出「PaidAt 為空且 ExpectedDate 在 N 天內」的紀錄。
/// 2026-09 納入預支沖銷差額分期（WriteOffInstallments）—— 該筆撥款過去完全沒有任何主動通知，
/// 財務只能自己去「已核准」頁籤撈。沖銷單本身無 ProjectId，專案代號取自其母預支單。
/// </summary>
public interface IPaymentReminderReadService
{
    Task<IReadOnlyList<UpcomingInstallmentDto>> GetUpcomingAsync(DateOnly fromDate, DateOnly toDate);
}

public sealed record UpcomingInstallmentDto(
    string    ApplicationType,      // payment_request / advance / travel / travel_payment / write_off
    int       ApplicationId,
    int       InstallmentNo,
    DateTime  ExpectedDate,
    decimal   Amount,
    string?   Note,
    string?   ProjectCode,
    string?   ApplicantName);

public sealed class PaymentReminderReadService(IDbConnection db) : IPaymentReminderReadService
{
    public async Task<IReadOnlyList<UpcomingInstallmentDto>> GetUpcomingAsync(DateOnly fromDate, DateOnly toDate)
    {
        const string sql = """
            SELECT 'payment_request' AS ApplicationType, i.PaymentRequestId AS ApplicationId,
                   i.InstallmentNo, i.ExpectedDate, i.Amount, i.Note,
                   proj.Code AS ProjectCode, sub.Name AS ApplicantName
            FROM PaymentRequestInstallments i
            JOIN PaymentRequests pr ON i.PaymentRequestId = pr.Id
            LEFT JOIN Projects proj ON pr.ProjectId = proj.Id
            LEFT JOIN Users sub     ON pr.SubmittedById = sub.Id
            WHERE i.PaidAt IS NULL
              AND CAST(i.ExpectedDate AS DATE) BETWEEN @FromDate AND @ToDate
              AND pr.ApprovalStatus = 'approved'

            UNION ALL

            SELECT 'advance' AS ApplicationType, i.AdvanceRequestId AS ApplicationId,
                   i.InstallmentNo, i.ExpectedDate, i.Amount, i.Note,
                   proj.Code AS ProjectCode, sub.Name AS ApplicantName
            FROM AdvanceRequestInstallments i
            JOIN AdvanceRequests ar ON i.AdvanceRequestId = ar.Id
            LEFT JOIN Projects proj ON ar.ProjectId = proj.Id
            LEFT JOIN Users sub     ON ar.SubmittedById = sub.Id
            WHERE i.PaidAt IS NULL
              AND CAST(i.ExpectedDate AS DATE) BETWEEN @FromDate AND @ToDate
              AND ar.ApprovalStatus = 'approved'

            UNION ALL

            SELECT 'travel' AS ApplicationType, i.TravelRequestId AS ApplicationId,
                   i.InstallmentNo, i.ExpectedDate, i.Amount, i.Note,
                   proj.Code AS ProjectCode, emp.Name AS ApplicantName
            FROM TravelRequestInstallments i
            JOIN TravelRequests tr ON i.TravelRequestId = tr.Id
            LEFT JOIN Projects proj ON tr.ProjectId = proj.Id
            LEFT JOIN Users emp     ON tr.EmployeeId = emp.Id
            WHERE i.PaidAt IS NULL
              AND CAST(i.ExpectedDate AS DATE) BETWEEN @FromDate AND @ToDate
              AND tr.ApprovalStatus = 'approved'

            UNION ALL

            SELECT 'travel_payment' AS ApplicationType, i.TravelPaymentRequestId AS ApplicationId,
                   i.InstallmentNo, i.ExpectedDate, i.Amount, i.Note,
                   proj.Code AS ProjectCode, emp.Name AS ApplicantName
            FROM TravelPaymentRequestInstallments i
            JOIN TravelPaymentRequests tpr ON i.TravelPaymentRequestId = tpr.Id
            LEFT JOIN Projects proj ON tpr.ProjectId = proj.Id
            LEFT JOIN Users emp     ON tpr.EmployeeId = emp.Id
            WHERE i.PaidAt IS NULL
              AND CAST(i.ExpectedDate AS DATE) BETWEEN @FromDate AND @ToDate
              AND tpr.ApprovalStatus = 'approved'

            UNION ALL

            SELECT 'write_off' AS ApplicationType, i.WriteOffRecordId AS ApplicationId,
                   i.InstallmentNo, i.ExpectedDate, i.Amount, i.Note,
                   proj.Code AS ProjectCode, sub.Name AS ApplicantName
            FROM WriteOffInstallments i
            JOIN WriteOffRecords wo ON i.WriteOffRecordId = wo.Id
            JOIN AdvanceRequests ar ON wo.AdvanceRequestId = ar.Id
            LEFT JOIN Projects proj ON ar.ProjectId = proj.Id
            LEFT JOIN Users sub     ON wo.SubmittedById = sub.Id
            WHERE i.PaidAt IS NULL
              AND CAST(i.ExpectedDate AS DATE) BETWEEN @FromDate AND @ToDate
              AND wo.ApprovalStatus = 'approved'

            ORDER BY ExpectedDate, ApplicationType, ApplicationId, InstallmentNo
            """;

        var rows = await db.QueryAsync<dynamic>(sql, new { FromDate = fromDate, ToDate = toDate });
        return rows.Select(r => new UpcomingInstallmentDto(
            (string)r.ApplicationType,
            (int)r.ApplicationId,
            (int)r.InstallmentNo,
            (DateTime)r.ExpectedDate,
            (decimal)r.Amount,
            (string?)r.Note,
            (string?)r.ProjectCode,
            (string?)r.ApplicantName)).ToList();
    }
}
