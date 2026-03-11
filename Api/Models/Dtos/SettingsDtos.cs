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
    int    MonthlyOvertimeLimit);

public sealed record UpdateSettingsRequest(
    string? SiteName                 = null,
    string? SiteUrl                  = null,
    string? ContactEmail             = null,
    string? SiteDescription          = null,
    string? Language                 = null,
    string? Timezone                 = null,
    int?    SessionTimeoutMinutes    = null,
    bool?   AllowRegistration        = null,
    bool?   RequireEmailVerification = null,
    bool?   MaintenanceMode          = null,
    string? MaintenanceMessage       = null,
    string? WorkStartTime            = null,
    string? WorkEndTime              = null,
    int?    MonthlyOvertimeLimit     = null);
