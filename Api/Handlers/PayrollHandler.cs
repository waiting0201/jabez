using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

/// <summary>
/// 人事薪資：月薪計算 + 薪資調整（其他扣項、備註）CRUD + 寄送薪資明細
/// </summary>
public sealed class PayrollHandler(IPayrollReadService reader, AppDbContext db, IEmailService emailService)
{
    /// <summary>GET /payroll?year=YYYY&amp;month=MM → 計算指定月份所有在職員工薪資</summary>
    public async Task<IActionResult> GetMonthlyAsync(HttpRequest req)
    {
        if (!int.TryParse(req.Query["year"], out var year) || year < 2000 || year > 2100)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份參數 (year)。"));

        if (!int.TryParse(req.Query["month"], out var month) || month < 1 || month > 12)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的月份參數 (month)。"));

        var result = await reader.CalculateMonthlyPayrollAsync(year, month);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>GET /payroll/{employeeId}/adjustment?year=YYYY&amp;month=MM → 取得單一員工薪資調整</summary>
    public async Task<IActionResult> GetAdjustmentAsync(HttpRequest req, string employeeId)
    {
        if (!Guid.TryParse(employeeId, out var empId))
            return new BadRequestObjectResult(ApiResponse.Fail("無效的員工 ID。"));

        if (!int.TryParse(req.Query["year"], out var year) || !int.TryParse(req.Query["month"], out var month))
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份與月份參數。"));

        var adj = await db.PayrollAdjustments
            .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Year == year && a.Month == month);

        if (adj is null)
            return new OkObjectResult(ApiResponse.Ok<PayrollAdjustmentDto?>(null));

        return new OkObjectResult(ApiResponse.Ok(ToDto(adj)));
    }

    /// <summary>PUT /payroll/{employeeId}/adjustment?year=YYYY&amp;month=MM → 新增或更新薪資調整</summary>
    public async Task<IActionResult> UpsertAdjustmentAsync(HttpRequest req, string employeeId)
    {
        if (!Guid.TryParse(employeeId, out var empId))
            return new BadRequestObjectResult(ApiResponse.Fail("無效的員工 ID。"));

        if (!int.TryParse(req.Query["year"], out var year) || !int.TryParse(req.Query["month"], out var month))
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份與月份參數。"));

        var body = await req.ReadFromJsonAsync<PayrollAdjustmentRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的請求內容。"));

        var now = Clock.Now;
        var adj = await db.PayrollAdjustments
            .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Year == year && a.Month == month);

        if (adj is null)
        {
            adj = new PayrollAdjustment
            {
                EmployeeId         = empId,
                Year               = year,
                Month              = month,
                OtherAddition      = body.OtherAddition,
                OtherAdditionNote  = body.OtherAdditionNote,
                OtherDeduction     = body.OtherDeduction,
                OtherDeductionNote = body.OtherDeductionNote,
                Note               = body.Note,
                CreatedAt          = now,
                UpdatedAt          = now,
            };
            db.PayrollAdjustments.Add(adj);
        }
        else
        {
            adj.OtherAddition      = body.OtherAddition;
            adj.OtherAdditionNote  = body.OtherAdditionNote;
            adj.OtherDeduction     = body.OtherDeduction;
            adj.OtherDeductionNote = body.OtherDeductionNote;
            adj.Note               = body.Note;
            adj.UpdatedAt          = now;
        }

        await db.SaveChangesAsync();
        return new OkObjectResult(ApiResponse.Ok(ToDto(adj), adj.Id == 0 ? "已新增薪資調整。" : "已更新薪資調整。"));
    }

    /// <summary>POST /payroll/send-slips?year=YYYY&amp;month=MM → 寄送薪資明細給有勾選的員工</summary>
    public async Task<IActionResult> SendSlipsAsync(HttpRequest req)
    {
        if (!int.TryParse(req.Query["year"], out var year) || year < 2000 || year > 2100)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份參數 (year)。"));
        if (!int.TryParse(req.Query["month"], out var month) || month < 1 || month > 12)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的月份參數 (month)。"));

        var result = await reader.CalculateMonthlyPayrollAsync(year, month);
        var targets = result.Employees
            .Where(e => e.SendPaySlip && !string.IsNullOrWhiteSpace(e.Email))
            .ToList();

        if (targets.Count == 0)
            return new OkObjectResult(ApiResponse.Ok<object?>(null, "沒有需要寄送的員工（請確認員工已勾選「寄送薪資明細」且有設定 Email）。"));

        int sent = 0;
        var errors = new List<string>();

        foreach (var emp in targets)
        {
            try
            {
                var subject = $"薪資明細 — {year} 年 {month} 月";
                var html = BuildPaySlipHtml(emp, year, month);
                await emailService.SendAsync(emp.Email!, subject, html);
                sent++;
            }
            catch (Exception ex)
            {
                errors.Add($"{emp.EmployeeName}：{ex.Message}");
            }
        }

        var msg = $"已寄送 {sent}/{targets.Count} 封薪資明細。";
        if (errors.Count > 0) msg += $" 失敗：{string.Join("；", errors)}";
        return new OkObjectResult(ApiResponse.Ok(new { sent, total = targets.Count, errors }, msg));
    }

    /// <summary>產生薪資明細 HTML 信件</summary>
    private static string BuildPaySlipHtml(EmployeePayrollDto emp, int year, int month)
    {
        var fmt = (decimal n) => n.ToString("N0");
        var hireDateStr = emp.HireDate?.ToString("yyyy/MM/dd") ?? "---";
        var totalEarnings = emp.BaseSalary + emp.MealAllowance + emp.OvertimePay + emp.HolidayAllowance + emp.OtherAddition;
        var totalDeductions = emp.LaborInsurance + emp.HealthInsurance
                            + emp.PersonalLeaveDeduction + emp.SickLeaveDeduction
                            + emp.OtherDeduction;

        var earningsRows = $"""
            <tr><td style="padding:8px 12px">底薪</td><td style="padding:8px 12px;text-align:right">{fmt(emp.BaseSalary)}</td></tr>
            <tr style="background:#FDFAF5"><td style="padding:8px 12px">伙食費</td><td style="padding:8px 12px;text-align:right">{fmt(emp.MealAllowance)}</td></tr>
            <tr><td style="padding:8px 12px">加班費</td><td style="padding:8px 12px;text-align:right">{fmt(emp.OvertimePay)}</td></tr>
            <tr style="background:#FDFAF5"><td style="padding:8px 12px">假日津貼（{emp.HolidayTravelDays} 天）</td><td style="padding:8px 12px;text-align:right">{fmt(emp.HolidayAllowance)}</td></tr>
            """;
        if (emp.OtherAddition > 0)
            earningsRows += $"""
            <tr><td style="padding:8px 12px">其他加項{(emp.OtherAdditionNote is not null ? $"（{emp.OtherAdditionNote}）" : "")}</td><td style="padding:8px 12px;text-align:right">{fmt(emp.OtherAddition)}</td></tr>
            """;

        var deductionRows = $"""
            <tr><td style="padding:8px 12px">勞保費（員工負擔）</td><td style="padding:8px 12px;text-align:right">{fmt(emp.LaborInsurance)}</td></tr>
            <tr style="background:#FDF5F5"><td style="padding:8px 12px">健保費（員工負擔）</td><td style="padding:8px 12px;text-align:right">{fmt(emp.HealthInsurance)}</td></tr>
            """;
        if (emp.PersonalLeaveDays > 0)
            deductionRows += $"""
            <tr><td style="padding:8px 12px">事假扣薪（{emp.PersonalLeaveDays} 天）</td><td style="padding:8px 12px;text-align:right">{fmt(emp.PersonalLeaveDeduction)}</td></tr>
            """;
        if (emp.SickLeaveDays > 0)
            deductionRows += $"""
            <tr style="background:#FDF5F5"><td style="padding:8px 12px">病假扣薪（{emp.SickLeaveDays} 天 × 半薪）</td><td style="padding:8px 12px;text-align:right">{fmt(emp.SickLeaveDeduction)}</td></tr>
            """;
        if (emp.OtherDeduction > 0)
            deductionRows += $"""
            <tr><td style="padding:8px 12px">其他扣項{(emp.OtherDeductionNote is not null ? $"（{emp.OtherDeductionNote}）" : "")}</td><td style="padding:8px 12px;text-align:right">{fmt(emp.OtherDeduction)}</td></tr>
            """;

        var noteSection = string.IsNullOrWhiteSpace(emp.Note) ? "" : $"""
            <div style="margin-top:16px;padding:12px 16px;background:#F5F2ED;border-radius:6px;color:#8C7355;font-size:13px">
                <strong>備註：</strong>{emp.Note}
            </div>
            """;

        return $"""
            <div style="font-family:'Microsoft JhengHei','Noto Sans TC',sans-serif;max-width:600px;margin:0 auto;color:#525358">
                <div style="border-top:4px solid #699F34;padding:24px 0 16px">
                    <h2 style="color:#699F34;margin:0 0 4px">薪資明細</h2>
                    <p style="color:#A39685;margin:0;font-size:14px">{year} 年 {month} 月</p>
                </div>

                <table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:14px">
                    <tr style="background:#F5F2ED">
                        <td style="padding:8px 12px;font-weight:bold;width:40%">姓名</td>
                        <td style="padding:8px 12px">{emp.EmployeeName}</td>
                    </tr>
                    <tr>
                        <td style="padding:8px 12px;font-weight:bold">部門 / 職稱</td>
                        <td style="padding:8px 12px">{emp.DepartmentName ?? "---"} / {emp.JobTitleName ?? "---"}</td>
                    </tr>
                    <tr style="background:#F5F2ED">
                        <td style="padding:8px 12px;font-weight:bold">到職日</td>
                        <td style="padding:8px 12px">{hireDateStr}</td>
                    </tr>
                </table>

                <h3 style="color:#699F34;font-size:15px;margin:20px 0 8px;border-bottom:2px solid #699F34;padding-bottom:4px">應發項目</h3>
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                    {earningsRows}
                    <tr style="background:#E8F0E4;font-weight:bold">
                        <td style="padding:8px 12px;color:#4A6B3A">應發合計</td>
                        <td style="padding:8px 12px;text-align:right;color:#4A6B3A">{fmt(totalEarnings)}</td>
                    </tr>
                </table>

                <h3 style="color:#A04040;font-size:15px;margin:20px 0 8px;border-bottom:2px solid #A04040;padding-bottom:4px">扣款項目</h3>
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                    {deductionRows}
                    <tr style="background:#F8E6E6;font-weight:bold">
                        <td style="padding:8px 12px;color:#A04040">扣款合計</td>
                        <td style="padding:8px 12px;text-align:right;color:#A04040">-{fmt(totalDeductions)}</td>
                    </tr>
                </table>

                <table style="width:100%;border-collapse:collapse;margin:20px 0;background:#699F34;color:#fff;border-radius:8px">
                    <tr>
                        <td style="padding:16px 20px;font-size:16px;font-weight:bold;text-align:left">實領薪資</td>
                        <td style="padding:16px 20px;font-size:22px;font-weight:bold;text-align:right">NT$ {fmt(emp.NetSalary)}</td>
                    </tr>
                </table>

                {noteSection}

                <hr style="border:none;border-top:1px solid #DDD6C8;margin:24px 0">
                <p style="color:#A39685;font-size:11px;margin:0">此為系統自動寄發之薪資明細，僅供參考。如有疑問請洽人事部門。</p>
            </div>
            """;
    }

    private static PayrollAdjustmentDto ToDto(PayrollAdjustment a) => new(
        a.Id, a.EmployeeId, a.Year, a.Month,
        a.OtherAddition, a.OtherAdditionNote,
        a.OtherDeduction, a.OtherDeductionNote, a.Note);
}
