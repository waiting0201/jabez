namespace Jabez.Api.Models.Dtos;

public sealed record SystemSettingsDto(
    string SiteName,
    string SiteUrl,
    string ContactEmail,
    string SiteDescription,
    string Language,
    string Timezone,
    int    SessionTimeoutMinutes,
    bool   AllowRegistration,
    bool   RequireEmailVerification,
    bool   MaintenanceMode,
    string MaintenanceMessage,
    string WorkStartTime,
    string WorkEndTime,
    int    MonthlyOvertimeLimit,
    bool   ApprovalEmailEnabled,
    bool   ApprovalLineEnabled,
    int    PaymentReminderDaysBefore,
    DateTime? FlexibleWorkStartDate);

public sealed record UpdateSettingsRequest(
    string? SiteName                  = null,
    string? SiteUrl                   = null,
    string? ContactEmail              = null,
    string? SiteDescription           = null,
    string? Language                  = null,
    string? Timezone                  = null,
    int?    SessionTimeoutMinutes     = null,
    bool?   AllowRegistration         = null,
    bool?   RequireEmailVerification  = null,
    bool?   MaintenanceMode           = null,
    string? MaintenanceMessage        = null,
    string? WorkStartTime             = null,
    string? WorkEndTime               = null,
    int?    MonthlyOvertimeLimit      = null,
    bool?   ApprovalEmailEnabled      = null,
    bool?   ApprovalLineEnabled       = null,
    int?    PaymentReminderDaysBefore = null,
    // 四週彈性工時切換日。null 於此代表「本次請求不變更」，
    // 要「清空切換日（退回舊制）」請另傳 ClearFlexibleWorkStartDate = true。
    DateTime? FlexibleWorkStartDate = null,
    bool?   ClearFlexibleWorkStartDate = null);
