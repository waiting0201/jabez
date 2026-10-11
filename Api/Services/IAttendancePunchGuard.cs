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
    /// 後續業務檢查（例如「今日已打上班卡」）丟例外時成功紀錄自然不落地、挑戰碼也不會被消耗。
    /// </summary>
    Task GuardAsync(HttpRequest req, Guid userId, string action, ClockActionRequest body);
}
