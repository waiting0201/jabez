using Jabez.Api.Models.Dtos;
using Microsoft.AspNetCore.Http;

namespace Jabez.Api.Services;

/// <summary>防機器人打卡守門（挑戰碼 + 強制 GPS + 嘗試紀錄），見 <see cref="AttendancePunchGuard"/></summary>
public interface IAttendancePunchGuard
{
    /// <summary>簽發一次性打卡挑戰碼（綁定使用者 + 動作）</summary>
    ClockChallengeDto IssueChallenge(Guid userId, string action);

    /// <summary>
    /// 驗證本次打卡；不通過時寫入一筆失敗紀錄並 SaveChanges 後丟 400。
    /// 通過時只把成功紀錄加入 DbContext（不 SaveChanges），由呼叫端與打卡紀錄同一次寫入 ——
    /// 後續業務檢查（例如「今日已打上班卡」）丟例外時成功紀錄自然不落地、挑戰碼也不會被消耗，
    /// 呼叫端須改呼叫 <see cref="RecordRejectionAsync"/> 把這次嘗試記成失敗。
    /// </summary>
    Task GuardAsync(HttpRequest req, Guid userId, string action, ClockActionRequest body);

    /// <summary>
    /// 通過關卡後打卡仍失敗（業務檢查丟 400，或寫入時發生非預期錯誤）時，把這次嘗試寫成一筆失敗紀錄（2026-11）。
    /// 會先清空 DbContext 未落地的變更再單獨寫入；本次請求沒有「已通過關卡、尚未落地」的紀錄時不做事。
    /// **絕不丟例外** —— 呼叫端要把原本的錯誤照常往外丟。
    /// </summary>
    Task RecordRejectionAsync(Exception ex);
}
