namespace Jabez.Api.Models.Entities;

public class CalendarDay
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public bool IsHoliday { get; set; }
    public string Description { get; set; } = "";
    public int Year { get; set; }
}
