using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services;

/// <summary>
/// 預支 / 出差預支「金額歷程」讀取服務（Dapper，唯讀）。
/// 全站「已沖銷 / 尚未結清 / 實際金額 / 差額」的單一真相：只計已核准沖銷，簽核中另列不進累計。
/// 事件時序：預支批次 → 預支撥款 → 已核准沖銷 → 同仁繳回 → 補撥超支 → 結案（Closed 恆為最後一列）。
/// </summary>
public interface IAdvanceSettlementLedgerService
{
    Task<AdvanceSettlementLedgerDto?> GetForAdvanceAsync(int advanceRequestId);
    Task<AdvanceSettlementLedgerDto?> GetForTravelAsync(int travelRequestId);
}

public sealed class AdvanceSettlementLedgerService(IDbConnection db) : IAdvanceSettlementLedgerService
{
    // 同時間點事件的排序權重（closed 另外強制放最後）
    private static readonly Dictionary<string, int> KindOrder = new()
    {
        ["advance_round"] = 0, ["advance_paid"] = 1, ["write_off"] = 2, ["returned"] = 3, ["overspend_paid"] = 4,
    };

    private sealed record RawEvent(DateTime? Date, string Kind, string Label, string? RequestNo, int? WriteOffId,
                                   decimal Amount, string? Note);

    public async Task<AdvanceSettlementLedgerDto?> GetForAdvanceAsync(int advanceRequestId)
    {
        const string sql = """
            SELECT a.GrandTotal, a.CurrentRoundNo, a.ApprovalStatus, a.AdvanceDate,
                   a.IsClosed, a.ClosedAt, a.ActualAmount, cu.Name AS ClosedByName
            FROM AdvanceRequests a
            LEFT JOIN Users cu ON cu.Id = a.ClosedById
            WHERE a.Id = @Id;

            SELECT RoundNo, SUM(TotalPrice) AS Total FROM AdvanceRequestItems
            WHERE AdvanceRequestId = @Id GROUP BY RoundNo;

            SELECT RoundNo, AdvanceDate FROM AdvanceRequestSupplements
            WHERE AdvanceRequestId = @Id;

            SELECT InstallmentNo, PaidAt, Amount FROM AdvanceRequestInstallments
            WHERE AdvanceRequestId = @Id AND PaidAt IS NOT NULL;

            SELECT Id, RequestNo, WriteOffNo, GrandTotal, ApprovalStatus,
                   COALESCE(ReviewedAt, SubmittedAt, CreatedAt) AS EventDate,
                   ReturnedAmount, ReturnedAt
            FROM WriteOffRecords WHERE AdvanceRequestId = @Id;

            SELECT i.PaidAt, i.Amount, w.Id AS WriteOffId, w.RequestNo, w.WriteOffNo
            FROM WriteOffInstallments i
            JOIN WriteOffRecords w ON w.Id = i.WriteOffRecordId
            WHERE w.AdvanceRequestId = @Id AND w.ApprovalStatus = 'approved' AND i.PaidAt IS NOT NULL;
            """;

        using var multi = await db.QueryMultipleAsync(sql, new { Id = advanceRequestId });
        var head = await multi.ReadSingleOrDefaultAsync<dynamic>();
        if (head is null) return null;
        var roundTotals = (await multi.ReadAsync<dynamic>()).ToDictionary(r => (int)r.RoundNo, r => (decimal)r.Total);
        var supplementDates = (await multi.ReadAsync<dynamic>()).ToDictionary(r => (int)r.RoundNo, r => (DateTime)r.AdvanceDate);
        var paid = (await multi.ReadAsync<dynamic>()).ToList();
        var writeOffs = (await multi.ReadAsync<dynamic>()).ToList();
        var overspend = (await multi.ReadAsync<dynamic>()).ToList();

        string status   = head.ApprovalStatus;
        int currentRound = (int)head.CurrentRoundNo;
        var events = new List<RawEvent>();

        // 預支批次：進行中的追加批次（尚未核准）不列、不計入預支總額
        decimal advanceTotal = 0m;
        foreach (var roundNo in roundTotals.Keys.OrderBy(k => k))
        {
            if (roundNo > 1 && roundNo >= currentRound && status != "approved") continue;
            var amount = roundTotals[roundNo];
            advanceTotal += amount;
            events.Add(new RawEvent(
                roundNo == 1 ? (DateTime)head.AdvanceDate : supplementDates.GetValueOrDefault(roundNo, (DateTime)head.AdvanceDate),
                "advance_round", roundNo == 1 ? "第1次預支" : $"第{roundNo}次追加", null, null, amount, null));
        }
        if (roundTotals.Count == 0) advanceTotal = (decimal)head.GrandTotal;

        foreach (var p in paid)
            events.Add(new RawEvent((DateTime?)p.PaidAt, "advance_paid", $"預支撥款（第{(int)p.InstallmentNo}期）", null, null, (decimal)p.Amount, null));

        decimal approvedTotal = 0m, returnedTotal = 0m, pendingTotal = 0m;
        foreach (var w in writeOffs)
        {
            string wst = w.ApprovalStatus;
            decimal gt = (decimal)w.GrandTotal;
            if (wst == "approved")
            {
                approvedTotal += gt;
                events.Add(new RawEvent((DateTime?)w.EventDate, "write_off", $"第{(int)w.WriteOffNo}次沖銷", (string?)w.RequestNo, (int)w.Id, gt, null));
                var ret = (decimal?)w.ReturnedAmount ?? 0m;
                if (ret > 0)
                {
                    returnedTotal += ret;
                    events.Add(new RawEvent((DateTime?)w.ReturnedAt ?? (DateTime?)w.EventDate, "returned",
                        $"同仁繳回（第{(int)w.WriteOffNo}次沖銷）", (string?)w.RequestNo, (int)w.Id, ret, null));
                }
            }
            else if (wst != "rejected")
            {
                pendingTotal += gt;
            }
        }

        decimal overspendPaidTotal = 0m;
        foreach (var o in overspend)
        {
            var amt = (decimal)o.Amount;
            overspendPaidTotal += amt;
            events.Add(new RawEvent((DateTime?)o.PaidAt, "overspend_paid", $"公司補撥超支（第{(int)o.WriteOffNo}次沖銷）",
                (string?)o.RequestNo, (int)o.WriteOffId, amt, null));
        }

        return Build(advanceTotal, approvedTotal, returnedTotal, overspendPaidTotal, pendingTotal,
            (bool)head.IsClosed, (DateTime?)head.ClosedAt, (string?)head.ClosedByName, (decimal?)head.ActualAmount, events);
    }

    public async Task<AdvanceSettlementLedgerDto?> GetForTravelAsync(int travelRequestId)
    {
        const string sql = """
            SELECT t.GrandTotal, t.ApprovalStatus, COALESCE(t.ReviewedAt, t.SubmittedAt, t.CreatedAt) AS EventDate,
                   t.IsClosed, t.ClosedAt, t.ActualAmount, cu.Name AS ClosedByName
            FROM TravelRequests t
            LEFT JOIN Users cu ON cu.Id = t.ClosedById
            WHERE t.Id = @Id;

            SELECT InstallmentNo, PaidAt, Amount FROM TravelRequestInstallments
            WHERE TravelRequestId = @Id AND PaidAt IS NOT NULL;

            SELECT Id, RequestNo, WriteOffNo, GrandTotal, ApprovalStatus,
                   COALESCE(ReviewedAt, SubmittedAt, CreatedAt) AS EventDate
            FROM TravelWriteOffRecords WHERE TravelRequestId = @Id;
            """;

        using var multi = await db.QueryMultipleAsync(sql, new { Id = travelRequestId });
        var head = await multi.ReadSingleOrDefaultAsync<dynamic>();
        if (head is null) return null;
        var paid = (await multi.ReadAsync<dynamic>()).ToList();
        var writeOffs = (await multi.ReadAsync<dynamic>()).ToList();

        var events = new List<RawEvent>();
        decimal advanceTotal = (decimal)head.GrandTotal;
        events.Add(new RawEvent((DateTime?)head.EventDate, "advance_round", "出差預支", null, null, advanceTotal, null));

        foreach (var p in paid)
            events.Add(new RawEvent((DateTime?)p.PaidAt, "advance_paid", $"預支撥款（第{(int)p.InstallmentNo}期）", null, null, (decimal)p.Amount, null));

        decimal approvedTotal = 0m, pendingTotal = 0m;
        foreach (var w in writeOffs)
        {
            string wst = w.ApprovalStatus;
            decimal gt = (decimal)w.GrandTotal;
            if (wst == "approved")
            {
                approvedTotal += gt;
                events.Add(new RawEvent((DateTime?)w.EventDate, "write_off", $"第{(int)w.WriteOffNo}次沖銷", (string?)w.RequestNo, (int)w.Id, gt, null));
            }
            else if (wst != "rejected")
            {
                pendingTotal += gt;
            }
        }

        return Build(advanceTotal, approvedTotal, 0m, 0m, pendingTotal,
            (bool)head.IsClosed, (DateTime?)head.ClosedAt, (string?)head.ClosedByName, (decimal?)head.ActualAmount, events);
    }

    private static AdvanceSettlementLedgerDto Build(
        decimal advanceTotal, decimal approvedTotal, decimal returnedTotal, decimal overspendPaidTotal, decimal pendingTotal,
        bool isClosed, DateTime? closedAt, string? closedByName, decimal? actualAmount, List<RawEvent> raw)
    {
        var outstanding = advanceTotal - approvedTotal - returnedTotal + overspendPaidTotal;

        // 依日期排序（null 排前），同時間依事件種類；以原始順序作穩定排序鍵
        var ordered = raw
            .Select((e, i) => (e, i))
            .OrderBy(x => x.e.Date ?? DateTime.MinValue)
            .ThenBy(x => KindOrder.GetValueOrDefault(x.e.Kind, 9))
            .ThenBy(x => x.i)
            .Select(x => x.e)
            .ToList();

        if (isClosed)
        {
            var note = outstanding != 0m
                ? $"差額未處理 {Math.Abs(outstanding):#,##0.##} 元"
                : null;
            ordered.Add(new RawEvent(closedAt, "closed", "結案", null, null, actualAmount ?? approvedTotal, note));
        }

        decimal runAdv = 0m, runWo = 0m, runRet = 0m, runPaid = 0m;
        var events = new List<AdvanceSettlementEventDto>(ordered.Count);
        foreach (var e in ordered)
        {
            switch (e.Kind)
            {
                case "advance_round":   runAdv += e.Amount; break;
                case "write_off":       runWo  += e.Amount; break;
                case "returned":        runRet += e.Amount; break;
                case "overspend_paid":  runPaid += e.Amount; break;
            }
            events.Add(new AdvanceSettlementEventDto(
                e.Date, e.Kind, e.Label, e.RequestNo, e.WriteOffId, e.Amount,
                runAdv, runWo, runAdv - runWo - runRet + runPaid, e.Note));
        }

        return new AdvanceSettlementLedgerDto(
            advanceTotal, approvedTotal, returnedTotal, overspendPaidTotal, outstanding, pendingTotal,
            isClosed, closedAt, closedByName, actualAmount,
            advanceTotal - (actualAmount ?? approvedTotal), events);
    }
}
