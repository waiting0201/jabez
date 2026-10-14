namespace Jabez.Api.Common;

/// <summary>
/// 「補加班」判定的單一真相（2026-10）：加班日早於申請日。
/// 已送簽 → 加班日 &lt; 送簽日；草稿（SubmittedAt 為 null）→ 加班日 &lt; 今天（Clock.Today，台北時間）。
/// 不新增欄位，由既有 OvertimeDate / SubmittedAt 推導，舊單自動適用。
/// 兩個欄位都是 Clock.Now 寫入的台北 naive 時間，故在 C# 端比對，不用 SQL GETDATE（DB 為 UTC）。
/// </summary>
public static class OvertimeBackfill
{
    public static bool IsBackfill(DateTime overtimeDate, DateTime? submittedAt) =>
        overtimeDate.Date < (submittedAt?.Date ?? Clock.Today);
}
