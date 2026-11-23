namespace Jabez.Api.Models.Dtos;

public record CalendarDayDto(
    int Id,
    DateTime Date,
    bool IsHoliday,
    string Description,
    int Year);

public record CreateCalendarDayRequest(
    DateTime Date,
    bool IsHoliday,
    string Description = "");

public record UpdateCalendarDayRequest(
    bool? IsHoliday,
    string? Description);

/// <summary>加倍工資日（選舉投票日等），見 <see cref="Jabez.Api.Models.Entities.DoublePayDay"/>。</summary>
public record DoublePayDayDto(
    int Id,
    DateTime Date,
    string Name);

public record CreateDoublePayDayRequest(
    DateTime Date,
    string Name);
