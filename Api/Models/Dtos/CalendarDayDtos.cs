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
