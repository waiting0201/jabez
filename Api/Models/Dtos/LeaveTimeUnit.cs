namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 請假時間單位（表現層概念，儲存仍為小時）
/// Hour: 小時（事假/家庭照顧假/病假/產檢假/陪產假）
/// HalfDay: 半天 = 4 小時（特休/補休/高階主管假）
/// Day: 天 = 8 小時（公假/婚假/產假/喪假/歲時祭儀假/流產假系列）
/// </summary>
public enum LeaveTimeUnit
{
    Hour,
    HalfDay,
    Day,
}
