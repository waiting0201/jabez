namespace Jabez.Api.Models.Entities;

/// <summary>
/// 下班打卡時填寫的「當日工時專案佔比」（一列一專案）。
/// 同一筆 AttendanceRecord 的佔比合計恆為 100；工時總表以每天 8 小時 × Percentage / 100 計算
/// （見 WorkHoursReportHandler.HoursPerDay），不看實際上下班時間。
/// 當日可由本人整批替換，隔天起鎖定（無編輯入口）。
/// </summary>
public class AttendanceProjectAllocation
{
    public int     Id                 { get; set; }
    public int     AttendanceRecordId { get; set; }
    public int     ProjectId          { get; set; }
    /// <summary>佔比（%），&gt; 0 且 ≤ 100，decimal(5,2)</summary>
    public decimal Percentage         { get; set; }
    public int     SortOrder          { get; set; }

    // Navigation
    public AttendanceRecord? AttendanceRecord { get; set; }
    public Project?          Project          { get; set; }
}
