using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services;

/// <summary>
/// 預支 / 出差預支「金額歷程」讀取服務（Dapper，唯讀）。
/// 全站「已沖銷 / 尚未結清 / 實際金額 / 差額」的單一真相：只計已核准沖銷，簽核中另列不進累計。
/// 「尚未結清」走 <see cref="WriteOffRefundCalculator.Settle"/> 的保守互抵規則（現金差額與總額差額同方向取較小者）：
/// 月結由公司直接支付廠商，月結差額另列 CheckDifference、不算同仁應繳回 / 公司應補撥，結案也不標「差額未處理」；
/// 現金與月結互抵的部分（CashDifference ≠ Outstanding）同樣不計，只在結案列附註提醒確認欄位。
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

    /// <param name="CheckAmount">該列金額中的月結（支票）部分；只有 advance_round / write_off 會帶值。</param>
    private sealed record RawEvent(DateTime? Date, string Kind, string Label, string? RequestNo, int? WriteOffId,
                                   decimal Amount, string? Note, decimal CheckAmount = 0m);

    private static string? CheckNote(decimal check)
        => check > 0m ? $"含月結 {check:#,##0.##} 元（公司直接支付廠商）" : null;

    public async Task<AdvanceSettlementLedgerDto?> GetForAdvanceAsync(int advanceRequestId)
    {
        const string sql = """
            SELECT a.GrandTotal, a.CheckTotal, a.CurrentRoundNo, a.ApprovalStatus, a.AdvanceDate,
                   a.IsClosed, a.ClosedAt, a.ActualAmount, cu.Name AS ClosedByName
            FROM AdvanceRequests a
            LEFT JOIN Users cu ON cu.Id = a.ClosedById
            WHERE a.Id = @Id;

            SELECT RoundNo, SUM(TotalPrice) AS Total, SUM(CheckAmount) AS CheckTotal FROM AdvanceRequestItems
            WHERE AdvanceRequestId = @Id GROUP BY RoundNo;

            SELECT RoundNo, AdvanceDate FROM AdvanceRequestSupplements
            WHERE AdvanceRequestId = @Id;

            SELECT InstallmentNo, PaidAt, Amount FROM AdvanceRequestInstallments
            WHERE AdvanceRequestId = @Id AND PaidAt IS NOT NULL;

            SELECT Id, RequestNo, WriteOffNo, GrandTotal, CheckTotal, ApprovalStatus,
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
        var roundRows = (await multi.ReadAsync<dynamic>()).ToList();
        var roundTotals = roundRows.ToDictionary(r => (int)r.RoundNo, r => (decimal)r.Total);
        var roundChecks = roundRows.ToDictionary(r => (int)r.RoundNo, r => (decimal)r.CheckTotal);
        var supplementDates = (await multi.ReadAsync<dynamic>()).ToDictionary(r => (int)r.RoundNo, r => (DateTime)r.AdvanceDate);
        var paid = (await multi.ReadAsync<dynamic>()).ToList();
        var writeOffs = (await multi.ReadAsync<dynamic>()).ToList();
        var overspend = (await multi.ReadAsync<dynamic>()).ToList();

        string status   = head.ApprovalStatus;
        int currentRound = (int)head.CurrentRoundNo;
        var events = new List<RawEvent>();

        // 預支批次：進行中的追加批次（尚未核准）不列、不計入預支總額
        decimal advanceTotal = 0m, advanceCheck = 0m;
        foreach (var roundNo in roundTotals.Keys.OrderBy(k => k))
        {
            if (roundNo > 1 && roundNo >= currentRound && status != "approved") continue;
            var amount = roundTotals[roundNo];
            var check  = roundChecks[roundNo];
            advanceTotal += amount;
            advanceCheck += check;
            events.Add(new RawEvent(
                roundNo == 1 ? (DateTime)head.AdvanceDate : supplementDates.GetValueOrDefault(roundNo, (DateTime)head.AdvanceDate),
                "advance_round", roundNo == 1 ? "第1次預支" : $"第{roundNo}次追加", null, null, amount, CheckNote(check), check));
        }
        if (roundTotals.Count == 0)
        {
            advanceTotal = (decimal)head.GrandTotal;
            advanceCheck = (decimal)head.CheckTotal;
        }

        foreach (var p in paid)
            events.Add(new RawEvent((DateTime?)p.PaidAt, "advance_paid", $"預支撥款（第{(int)p.InstallmentNo}期）", null, null, (decimal)p.Amount, null));

        decimal approvedTotal = 0m, approvedCheck = 0m, returnedTotal = 0m, pendingTotal = 0m;
        foreach (var w in writeOffs)
        {
            string wst = w.ApprovalStatus;
            decimal gt = (decimal)w.GrandTotal;
            if (wst == "approved")
            {
                var check = (decimal)w.CheckTotal;
                approvedTotal += gt;
                approvedCheck += check;
                events.Add(new RawEvent((DateTime?)w.EventDate, "write_off", $"第{(int)w.WriteOffNo}次沖銷", (string?)w.RequestNo, (int)w.Id, gt, CheckNote(check), check));
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
            (bool)head.IsClosed, (DateTime?)head.ClosedAt, (string?)head.ClosedByName, (decimal?)head.ActualAmount, events,
            advanceCheck, approvedCheck);
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
        bool isClosed, DateTime? closedAt, string? closedByName, decimal? actualAmount, List<RawEvent> raw,
        decimal advanceCheck = 0m, decimal approvedCheck = 0m)
    {
        // 尚未結清 ＝ 現金差額與總額差額同方向取較小者（WriteOffRefundCalculator.Settle）；
        // 月結差額、現金 / 月結互抵的部分另列，不算同仁應繳回 / 公司應補撥
        var checkDifference = advanceCheck - approvedCheck;
        var totalDifference = advanceTotal - approvedTotal - returnedTotal + overspendPaidTotal;
        var cashDifference  = totalDifference - checkDifference;
        var outstanding     = WriteOffRefundCalculator.Settle(cashDifference, totalDifference);

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
            // 只有同仁與公司之間的差額才算「未處理」；月結差額是公司與廠商之間的事，僅附註說明
            var notes = new List<string>(3);
            if (outstanding != 0m)
                notes.Add($"差額未處理 {Math.Abs(outstanding):#,##0.##} 元");
            if (cashDifference != outstanding)
                notes.Add($"現金{(cashDifference > 0m ? "剩餘" : "超支")} {Math.Abs(cashDifference - outstanding):#,##0.##} 元與月結互抵，不列入應繳回 / 應補撥");
            if (checkDifference != 0m)
                notes.Add($"月結實支較預支{(checkDifference > 0m ? "少" : "多")} {Math.Abs(checkDifference):#,##0.##} 元（公司直接支付廠商，無須繳回 / 補撥）");
            var note = notes.Count > 0 ? string.Join("；", notes) : null;
            ordered.Add(new RawEvent(closedAt, "closed", "結案", null, null, actualAmount ?? approvedTotal, note));
        }

        decimal runAdv = 0m, runWo = 0m, runRet = 0m, runPaid = 0m, runAdvCheck = 0m, runWoCheck = 0m;
        var events = new List<AdvanceSettlementEventDto>(ordered.Count);
        foreach (var e in ordered)
        {
            switch (e.Kind)
            {
                case "advance_round":   runAdv += e.Amount; runAdvCheck += e.CheckAmount; break;
                case "write_off":       runWo  += e.Amount; runWoCheck  += e.CheckAmount; break;
                case "returned":        runRet += e.Amount; break;
                case "overspend_paid":  runPaid += e.Amount; break;
            }
            events.Add(new AdvanceSettlementEventDto(
                e.Date, e.Kind, e.Label, e.RequestNo, e.WriteOffId, e.Amount,
                runAdv, runWo, RunningOutstanding(runAdv, runAdvCheck, runWo, runWoCheck, runRet, runPaid), e.Note));
        }

        return new AdvanceSettlementLedgerDto(
            advanceTotal, approvedTotal, returnedTotal, overspendPaidTotal, outstanding, pendingTotal,
            isClosed, closedAt, closedByName, actualAmount,
            advanceTotal - (actualAmount ?? approvedTotal), events,
            advanceCheck, approvedCheck, checkDifference, cashDifference);
    }

    private static decimal RunningOutstanding(decimal adv, decimal advCheck, decimal wo, decimal woCheck, decimal ret, decimal paid)
    {
        var total = adv - wo - ret + paid;
        return WriteOffRefundCalculator.Settle(total - (advCheck - woCheck), total);
    }
}
