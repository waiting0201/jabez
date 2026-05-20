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
        if (body.WorkStartTime            is not null) entity.WorkStartTime            = body.WorkStartTime;
        if (body.WorkEndTime              is not null) entity.WorkEndTime              = body.WorkEndTime;
        if (body.MonthlyOvertimeLimit     is not null) entity.MonthlyOvertimeLimit     = body.MonthlyOvertimeLimit.Value;
        if (body.ApprovalEmailEnabled     is not null) entity.ApprovalEmailEnabled     = body.ApprovalEmailEnabled.Value;
        if (body.ApprovalLineEnabled      is not null) entity.ApprovalLineEnabled      = body.ApprovalLineEnabled.Value;
        if (body.PaymentReminderDaysBefore is not null) entity.PaymentReminderDaysBefore = Math.Clamp(body.PaymentReminderDaysBefore.Value, 0, 30);

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(ToDto(entity), "Settings updated."));
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
        PaymentReminderDaysBefore: e.PaymentReminderDaysBefore);
}
