using Jabez.Api.Common;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Jabez.Api.Handlers;

/// <summary>
/// 排班提醒手動觸發（Superadmin，除錯／補發用）。
/// 比照既有的 <c>AttendanceReminderAdminHandler</c> 與撥款提醒手動觸發。
///
/// POST /shift-schedule-reminders/run?kind=schOpen|schPending|schDeadline|schAuto[&send=true]
///
/// ⚠ **預設乾跑（只回收件人名單、不發送）**。要真的推播必須明確帶 <c>send=true</c> ——
/// 誤觸的代價是真實同仁收到看不懂的通知，且 LINE **不支援撤回**已送出的推播。
///
/// 指定 kind 時會**略過「今天是幾號、現在幾點」的判斷與同日去重** ——
/// 排程沒跑到時要補發、或上線前要驗證收件人名單，都需要這條路。
/// </summary>
public sealed class ShiftScheduleReminderAdminHandler(
    IShiftScheduleReminderService service,
    IAttendanceReminderReadService reminderReader,
    IWorkdayScheduleProvider workdaySchedule,
    IAutoShiftScheduleService autoScheduler,
    IJwtService jwtService)
{
    public async Task<IActionResult> RunAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");

        if (principal.FindFirst("is_superadmin")?.Value != "true")
            throw AppException.Forbidden("僅 Superadmin 可手動觸發排班提醒。");

        var kind = req.Query["kind"].ToString();
        if (string.IsNullOrWhiteSpace(kind))
            throw AppException.BadRequest("請指定 kind（schOpen / schPending / schDeadline / schAuto）。");
        if (Array.IndexOf(IShiftScheduleReminderService.ValidKinds, kind) < 0)
            throw AppException.BadRequest($"kind 必須為 {string.Join(" / ", IShiftScheduleReminderService.ValidKinds)}。");

        Guid? userId = Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var uid) ? uid : null;

        // ⚠ **預設乾跑**：這支端點的作用就是對外發 LINE，誤觸會讓真實同仁收到看不懂的通知，
        //    而 LINE 不支援撤回已送出的推播。要真的送必須明確帶 send=true。
        var send = req.Query["send"].ToString() == "true";

        var result = await service.RunAsync("manual", userId, kind, dryRun: !send);
        return new OkObjectResult(ApiResponse.Ok(result,
            send ? "排班提醒已發送。" : "乾跑完成（未發送任何訊息）。帶 send=true 才會真的推播。"));
    }

    /// <summary>
    /// **唯讀**預覽個人化下班提醒（GET /shift-schedule-reminders/clock-out-preview[?at=HH:mm]）。
    ///
    /// 完全不發送、不寫紀錄，只回答三件事：
    /// <list type="number">
    ///   <item>今天有哪些人打了上班卡、還沒打下班卡（<b>含 ClockInTime 的值</b> ——
    ///         舊制 SQL 只用 NOT EXISTS 判有沒有打卡，沒把值撈出來，新制算不出個人時點）</item>
    ///   <item>每個人的提醒時點（實際上班打卡 ＋ 9 小時 − 2 分；請上午半天假者 ＋4 小時）</item>
    ///   <item>此刻（或 <c>at</c> 指定的時刻）誰會被推、誰因**每人每日去重**而跳過</item>
    /// </list>
    ///
    /// <c>at</c> 可指定模擬時刻，才不必等到 17:33 才驗得了。
    /// </summary>
    public async Task<IActionResult> ClockOutPreviewAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");
        if (principal.FindFirst("is_superadmin")?.Value != "true")
            throw AppException.Forbidden("僅 Superadmin 可檢視。");

        var now = Clock.Now;
        if (TimeOnly.TryParse(req.Query["at"].ToString(), out var at))
            now = now.Date.Add(at.ToTimeSpan());

        var switchDate = await workdaySchedule.GetSwitchDateAsync();
        var flexible   = switchDate is { } sd && now.Date >= sd.Date;
        var schedule   = WorkdayHours.For(now.Date, switchDate);

        var candidates = await reminderReader.GetClockOutCandidatesAsync(now.Date);
        var pushed     = (await reminderReader.GetAlreadyPushedUserIdsAsync(now.Date, "clockOut")).ToHashSet();

        var rows = candidates.Select(c =>
        {
            var remindAt = ClockRules.ExpectedClockOut(c.ClockInTime, schedule, c.AfternoonOnly).AddMinutes(-2);
            var alreadyPushed = pushed.Contains(c.UserId);
            var due = !alreadyPushed && now >= remindAt && now < remindAt.AddMinutes(30);

            return new
            {
                c.UserName,
                ClockIn       = c.ClockInTime.ToString("HH:mm"),
                c.AfternoonOnly,
                ExpectedOut   = remindAt.AddMinutes(2).ToString("HH:mm"),
                RemindAt      = remindAt.ToString("HH:mm"),
                AlreadyPushed = alreadyPushed,
                WouldPushNow  = due,
            };
        }).OrderBy(x => x.RemindAt).ToList();

        return new OkObjectResult(ApiResponse.Ok(new
        {
            SimulatedNow = now.ToString("yyyy-MM-dd HH:mm"),
            FlexibleEnabled = flexible,
            CandidateCount = rows.Count,
            WouldPushCount = rows.Count(r => r.WouldPushNow),
            Rows = rows,
        }));
    }

    /// <summary>
    /// **唯讀**預覽自動排班（GET /shift-schedule-reminders/auto-schedule-preview?year=&amp;month=[&amp;userId=]）。
    ///
    /// 只算不寫：對每位逾期者跑一次演算法，回傳排出來的例假／休假日期，
    /// 以及**排不出合法班表**者與其原因（那些人一律留白不寫入，須人工處理）。
    /// 上線前可用這支確認演算法在真實的國定假日／活動日分佈下排得出東西。
    /// </summary>
    public async Task<IActionResult> AutoSchedulePreviewAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");
        if (principal.FindFirst("is_superadmin")?.Value != "true")
            throw AppException.Forbidden("僅 Superadmin 可檢視。");

        var now = Clock.Now;
        int year  = int.TryParse(req.Query["year"],  out var y) ? y : now.AddMonths(1).Year;
        int month = int.TryParse(req.Query["month"], out var m) ? m : now.AddMonths(1).Month;
        if (month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");

        Guid? onlyUser = Guid.TryParse(req.Query["userId"], out var uid) ? uid : null;

        var result = await autoScheduler.RunAsync(year, month, dryRun: true, onlyUserId: onlyUser);
        return new OkObjectResult(ApiResponse.Ok(result));
    }

    /// <summary>
    /// 實際執行自動排班（POST /shift-schedule-reminders/auto-schedule?year=&amp;month=[&amp;userId=][&amp;apply=true]）。
    ///
    /// **預設仍是乾跑**，要真的寫入必須明確帶 <c>apply=true</c>（同 run 端點的 send=true 慣例）。
    /// 與 26 號排程的差別：這支**不發任何通知**，純粹補跑排班 ——
    /// 排程沒跑到要補救時，通知與排班該分開處理。
    /// </summary>
    public async Task<IActionResult> AutoScheduleApplyAsync(HttpRequest req)
    {
        var principal = await jwtService.ValidateRequestAsync(req)
                        ?? throw AppException.Unauthorized("Invalid token.");
        if (principal.FindFirst("is_superadmin")?.Value != "true")
            throw AppException.Forbidden("僅 Superadmin 可執行。");

        var now = Clock.Now;
        int year  = int.TryParse(req.Query["year"],  out var y) ? y : now.AddMonths(1).Year;
        int month = int.TryParse(req.Query["month"], out var m) ? m : now.AddMonths(1).Month;
        if (month is < 1 or > 12) throw AppException.BadRequest("月份必須介於 1 ~ 12。");

        Guid? onlyUser = Guid.TryParse(req.Query["userId"], out var uid) ? uid : null;
        bool apply = req.Query["apply"].ToString() == "true";

        var result = await autoScheduler.RunAsync(year, month, dryRun: !apply, onlyUserId: onlyUser);
        return new OkObjectResult(ApiResponse.Ok(result,
            apply ? "自動排班已寫入（未發送任何通知）。" : "乾跑完成（未寫入）。帶 apply=true 才會真的寫入。"));
    }
}
