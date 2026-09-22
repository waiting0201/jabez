using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;

namespace Jabez.Api.Services.Dapper;

public interface IPaymentReportReadService
{
    /// <summary>
    /// 款項統計分頁列表。category 必填，6 個合法值：
    /// payment / advance / writeoff / travel-payment / travel / travel-writeoff
    /// </summary>
    Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        ProjectAccessScope scope,
        string category,
        int page, int pageSize,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null);

    /// <summary>
    /// 匯出用：主表 LEFT JOIN 子表，一列一明細；前端依 category 對應表頭。
    /// </summary>
    Task<List<PaymentExportRowDto>> GetExportRowsAsync(
        ProjectAccessScope scope,
        string category,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null);

    /// <summary>
    /// 待撥款清單：**一期一列**（與上面兩支的「一單一列」粒度不同）。
    /// 依**預計撥款日**區間查出各期，供財務排款並逐筆開啟簽核作業填實際撥款日。
    ///
    /// ⚠ 只含 `ApprovalStatus = 'approved'` 的單 —— 撥款明細端點僅開放 approved，
    ///   列出 pending 的單會讓財務點進去卻填不了實際撥款日。
    /// ⚠ `travel-writeoff` 無 installments 表，該類別一律回空清單。
    /// </summary>
    Task<PagedResult<DuePaymentRowDto>> GetDuePagedAsync(
        ProjectAccessScope scope,
        string category,
        int page, int pageSize,
        DateOnly? dueFrom = null, DateOnly? dueTo = null, string? installmentStatus = null);
}
