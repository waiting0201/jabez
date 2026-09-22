namespace Jabez.Api.Models.Entities;

/// <summary>
/// 主管（各部門協理）於活動 2 個月前預先排定的「活動日」，供同仁排自己的班時參考。
///
/// ⚠ **這是疊加旗標，不是第 5 種日別**：月曆格的狀態仍是四選一（上班日／例假日／休假日／國定假日），
/// 活動日壓在其上。自動排班要「跳過活動日」、總覽表要以第三色標示，兩者都預設它與狀態並存；
/// 做成第 5 種狀態會讓「跳過」與配額計算互相打架。
///
/// **活動日可以排在國定假日上**（2026-09-17 決議，因為假日活動本來就會排在國定假日）：
/// 國定假日格對同仁仍唯讀、仍不佔配額，但被勾為預定人力者當天**解鎖上下班打卡、不需加班申請單**，
/// 第 9 小時起才另提加班申請。這是活動日旗標唯一能疊加在「上班日」以外狀態上的情形。
///
/// **改期**：得因業主通知或天氣因素變更，協理於原活動日之前皆可改（不受 10–25 日開放期限制）。
/// 改期後系統對受影響同仁重跑排班三條檢核並通知，但**不自動改寫個人已定案的班表**。
/// </summary>
public class ActivityDay
{
    public int      Id           { get; set; }
    public DateTime Date         { get; set; }
    public int      DepartmentId { get; set; }
    public string   Title        { get; set; } = "";
    public Guid     CreatedByUserId { get; set; }
    public DateTime CreatedAt    { get; set; }
    public DateTime UpdatedAt    { get; set; }

    // Navigation
    public Department? Department { get; set; }
    public ICollection<ActivityDayAssignee> Assignees { get; set; } = [];
}
