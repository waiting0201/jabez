using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jabez.Api.Handlers;

/// <summary>
/// GET /reports/payment        → 款項統計報表（分頁）
/// GET /reports/payment/export → 款項統計匯出（不分頁、主表 LEFT JOIN 子表）
/// category 必填，6 個合法值：payment / advance / writeoff / travel-payment / travel / travel-writeoff
/// </summary>
public sealed class PaymentReportHandler(IPaymentReportReadService reader, IProjectAccessResolver access)
{
    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (category, dateFrom, dateTo, paymentStatus) = ParseFilters(req);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetPagedAsync(scope, category, page, pageSize, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetExportAsync(HttpRequest req)
    {
        var (category, dateFrom, dateTo, paymentStatus) = ParseFilters(req);

        var scope = await access.ResolveAsync(req.HttpContext.User);
        var rows = await reader.GetExportRowsAsync(scope, category, dateFrom, dateTo, paymentStatus);
        return new OkObjectResult(ApiResponse.Ok(rows));
    }

    /// <summary>
    /// GET /reports/payment/due —— 待撥款清單（**一期一列**）。
    ///
    /// 財務排款的入口：用預計撥款日區間查出各期，再逐筆開啟簽核作業填實際撥款日。
    /// 權限同其餘兩支（`reports-payment:read`），部門可視範圍一樣由 ProjectAccessScope 自動套用。
    /// </summary>
    public async Task<IActionResult> GetDueAsync(HttpRequest req)
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;

        string category = req.Query["category"].ToString();
        if (string.IsNullOrWhiteSpace(category))
            throw AppException.BadRequest("請選擇類別");
        if (!PaymentReportReadService.AllCategories.Contains(category))
            throw AppException.BadRequest("不支援的類別");

        DateOnly? dueFrom = DateOnly.TryParse(req.Query["dueFrom"], out var df) ? df : null;
        DateOnly? dueTo   = DateOnly.TryParse(req.Query["dueTo"],   out var dt) ? dt : null;

        // 白名單正規化：非法值一律退回 unpaid（本清單的用途就是「還沒撥的」），
        // 不要讓打錯的參數靜默變成「全部」而多列出已撥款的期數。
        string? installmentStatus = req.Query["installmentStatus"].ToString() switch
        {
            "paid" => "paid",
            "all"  => null,
            _      => "unpaid",
        };

        var scope  = await access.ResolveAsync(req.HttpContext.User);
        var result = await reader.GetDuePagedAsync(scope, category, page, pageSize, dueFrom, dueTo, installmentStatus);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    private static (string category, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus) ParseFilters(HttpRequest req)

    {
        string category = req.Query["category"].ToString();
        if (string.IsNullOrWhiteSpace(category))
            throw AppException.BadRequest("請選擇類別");
        if (!PaymentReportReadService.AllCategories.Contains(category))
            throw AppException.BadRequest("不支援的類別");

        DateOnly? dateFrom = DateOnly.TryParse(req.Query["dateFrom"], out var df) ? df : null;
        DateOnly? dateTo   = DateOnly.TryParse(req.Query["dateTo"],   out var dt) ? dt : null;
        // 白名單正規化（2026-09 由二態擴成四態）：非法值一律視為「全部」而非某一態，
        // 避免像舊 status 參數那樣靜默落到某個分支、使用者看到的卻是被過濾過的結果。
        string? paymentStatus = req.Query["paymentStatus"].ToString() switch
        {
            "paid"    => "paid",
            "partial" => "partial",
            "unpaid"  => "unpaid",
            _         => null,
        };
        return (category, dateFrom, dateTo, paymentStatus);
    }
}
