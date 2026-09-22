namespace Jabez.Api.Models.Entities;

/// <summary>
/// 改班申請的逐日明細 —— 「哪一天、從什麼改成什麼」。
///
/// <see cref="FromDayType"/> 是**送單當下的快照**，用途有二：
/// 1. 簽核者看得到「原本是什麼」，不必自己去查班表
/// 2. 核准時比對現況，若班表在送簽期間被別的途徑改過（例如當日臨時調休），可據此判斷是否仍適用
/// </summary>
public class ShiftChangeRequestDate
{
    public int      Id                   { get; set; }
    public int      ShiftChangeRequestId { get; set; }
    public DateTime Date                 { get; set; }
    /// <summary>送單當下的日別快照（work / rest_day / statutory_off）</summary>
    public string   FromDayType          { get; set; } = "";
    /// <summary>要改成的日別（work / rest_day / statutory_off）</summary>
    public string   ToDayType            { get; set; } = "";

    // Navigation
    public ShiftChangeRequest? ShiftChangeRequest { get; set; }
}
