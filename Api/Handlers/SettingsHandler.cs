using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

/// <summary>
/// 系統設定 CRUD（單行模式，Id = 1）。
/// </summary>
public sealed class SettingsHandler(AppDbContext db)
{
    private const int SettingsId = 1;

    public async Task<IActionResult> GetAsync()
    {
        var entity = await db.SystemSettings.FindAsync(SettingsId);
        if (entity is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Settings not found."));

        return new OkObjectResult(ApiResponse.Ok(ToDto(entity)));
    }

    /// <summary>
    /// GET /work-mode —— **輕量讀取端點**（任何登入者，免 `settings:read`）。
    ///
    /// 只回「四週彈性工時切換了沒」這一件事。存在理由：前端有數個地方要依制度分歧
    /// （目前是〈假日執行活動申請〉的新增入口），而 `GET /settings` 需要 `settings:read`——
    /// 讓一般同仁為了知道制度而拿到整份系統設定（含維護模式、通知開關）是把後台權限強加給員工，
    /// 正是 backend-design §13 要避免的事。
    ///
    /// 回傳刻意只有兩個欄位、皆非敏感：切換日本身與「以今天判斷是否已生效」。
    /// ⚠ `IsFlexibleActive` 是**以今天**判斷，僅供 UI 開關使用；
    ///   任何與**資料**有關的判定（薪資、加班費、請假時段）一律以**該筆資料自己的日期**比對切換日，
    ///   不可改用這個旗標，否則切換後歷史資料會被新制重新解讀。
    /// </summary>
    public async Task<IActionResult> GetWorkModeAsync()
    {
        var switchDate = await db.SystemSettings.AsNoTracking()
            .Where(s => s.Id == SettingsId)
            .Select(s => s.FlexibleWorkStartDate)
            .FirstOrDefaultAsync();

        return new OkObjectResult(ApiResponse.Ok(new
        {
            flexibleWorkStartDate = switchDate,
            isFlexibleActive      = WorkdayHours.IsFlexible(Clock.Now, switchDate),
        }));
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<UpdateSettingsRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var entity = await db.SystemSettings.FindAsync(SettingsId);
        if (entity is null)
            return new NotFoundObjectResult(ApiResponse.Fail("Settings not found."));

        // Patch：只更新有傳入的欄位
        if (body.SiteName                 is not null) entity.SiteName                 = body.SiteName;
        if (body.SiteUrl                  is not null) entity.SiteUrl                  = body.SiteUrl;
        if (body.ContactEmail             is not null) entity.ContactEmail             = body.ContactEmail;
        if (body.SiteDescription          is not null) entity.SiteDescription          = body.SiteDescription;
        if (body.Language                 is not null) entity.Language                 = body.Language;
        if (body.Timezone                 is not null) entity.Timezone                 = body.Timezone;
        if (body.SessionTimeoutMinutes    is not null) entity.SessionTimeoutMinutes    = body.SessionTimeoutMinutes.Value;
        if (body.AllowRegistration        is not null) entity.AllowRegistration        = body.AllowRegistration.Value;
        if (body.RequireEmailVerification is not null) entity.RequireEmailVerification = body.RequireEmailVerification.Value;
        if (body.MaintenanceMode          is not null) entity.MaintenanceMode          = body.MaintenanceMode.Value;
        if (body.MaintenanceMessage       is not null) entity.MaintenanceMessage       = body.MaintenanceMessage;
        // 上下班時間必須是 "HH:mm" —— 打卡提醒的時點判斷直接吃這兩個欄位，
        // 存進格式不合的值（空字串、"9am"…）不會噴錯，只會讓提醒從此靜默不發，
        // 且沒有任何 log 可循，故在入口就擋掉。
        if (body.WorkStartTime            is not null) entity.WorkStartTime            = NormalizeWorkTime(body.WorkStartTime, "上班時間");
        if (body.WorkEndTime              is not null) entity.WorkEndTime              = NormalizeWorkTime(body.WorkEndTime,   "下班時間");
        if (body.MonthlyOvertimeLimit     is not null) entity.MonthlyOvertimeLimit     = body.MonthlyOvertimeLimit.Value;
        if (body.ApprovalEmailEnabled     is not null) entity.ApprovalEmailEnabled     = body.ApprovalEmailEnabled.Value;
        if (body.ApprovalLineEnabled      is not null) entity.ApprovalLineEnabled      = body.ApprovalLineEnabled.Value;
        if (body.PaymentReminderDaysBefore is not null) entity.PaymentReminderDaysBefore = Math.Clamp(body.PaymentReminderDaysBefore.Value, 0, 30);
        // 四週彈性工時切換日：日期本身用 Patch 語意（null = 不變更），
        // 「清空」另走 ClearFlexibleWorkStartDate —— 否則退回舊制的意圖表達不出來。
        if (body.ClearFlexibleWorkStartDate == true) entity.FlexibleWorkStartDate = null;
        else if (body.FlexibleWorkStartDate is not null) entity.FlexibleWorkStartDate = body.FlexibleWorkStartDate.Value.Date;

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(ToDto(entity), "Settings updated."));
    }

    /// <summary>
    /// 驗證並正規化 "HH:mm"（容忍 "H:mm"，一律補零存回）。
    /// 格式不合直接 400，不讓壞值進 DB —— 消費端見 <c>AttendanceReminderService</c>。
    /// </summary>
    private static string NormalizeWorkTime(string value, string fieldLabel)
    {
        if (!TimeSpan.TryParseExact(value.Trim(), [@"h\:mm", @"hh\:mm"],
                System.Globalization.CultureInfo.InvariantCulture, out var t))
            throw AppException.BadRequest($"{fieldLabel}格式不正確，請填 HH:mm（例如 09:00）。");

        return $"{t.Hours:D2}:{t.Minutes:D2}";
    }

    private static SystemSettingsDto ToDto(Models.Entities.SystemSetting e) => new(
        SiteName:                 e.SiteName,
        SiteUrl:                  e.SiteUrl,
        ContactEmail:             e.ContactEmail,
        SiteDescription:          e.SiteDescription,
        Language:                 e.Language,
        Timezone:                 e.Timezone,
        SessionTimeoutMinutes:    e.SessionTimeoutMinutes,
        AllowRegistration:        e.AllowRegistration,
        RequireEmailVerification: e.RequireEmailVerification,
        MaintenanceMode:          e.MaintenanceMode,
        MaintenanceMessage:       e.MaintenanceMessage,
        WorkStartTime:            e.WorkStartTime,
        WorkEndTime:              e.WorkEndTime,
        MonthlyOvertimeLimit:     e.MonthlyOvertimeLimit,
        ApprovalEmailEnabled:     e.ApprovalEmailEnabled,
        ApprovalLineEnabled:      e.ApprovalLineEnabled,
        PaymentReminderDaysBefore: e.PaymentReminderDaysBefore,
        FlexibleWorkStartDate:     e.FlexibleWorkStartDate);
}
