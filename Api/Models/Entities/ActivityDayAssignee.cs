namespace Jabez.Api.Models.Entities;

/// <summary>
/// 活動日的「預定人力」。被列入者於該活動日：
/// 若當日為國定假日 → 解鎖上下班打卡、不需加班申請單（計酬走「國定假日出勤」）。
/// </summary>
public class ActivityDayAssignee
{
    public int  Id            { get; set; }
    public int  ActivityDayId { get; set; }
    public Guid UserId        { get; set; }

    // Navigation
    public ActivityDay? ActivityDay { get; set; }
    public User?        User        { get; set; }
}
