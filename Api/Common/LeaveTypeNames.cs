namespace Jabez.Api.Common;

/// <summary>
/// 假別中文名稱對照表（共用於請假衝突訊息、打卡阻擋訊息等）。
/// </summary>
public static class LeaveTypeNames
{
    private static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>
    {
        ["annual"]              = "特休假",
        ["personal"]            = "事假",
        ["sick"]                = "病假",
        ["compensatory"]        = "補休",
        ["official"]            = "公假",
        ["marriage"]            = "婚假",
        ["maternity"]           = "產假",
        ["miscarriage_3m"]      = "流產假(3個月以上)",
        ["miscarriage_2to3m"]   = "流產假(2-3個月)",
        ["miscarriage_under2m"] = "流產假(未滿2個月)",
        ["prenatal_checkup"]    = "產檢假",
        ["paternity"]           = "陪產假",
        ["bereavement"]         = "喪假",
        ["ceremonial_festival"] = "歲時祭儀假",
        ["senior_executive"]    = "高階主管假",
    };

    /// <summary>取得假別的中文名稱；找不到時回傳原字串。</summary>
    public static string GetZh(string leaveType) =>
        Map.TryGetValue(leaveType, out var name) ? name : leaveType;
}
