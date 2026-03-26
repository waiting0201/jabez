using System.Text.Json;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

public sealed class CalendarDayHandler(AppDbContext db, ICalendarDayReadService reader)
{
    private static readonly HttpClient _http = new();
    private const string CalendarApiUrl = "https://cdn.jsdelivr.net/gh/ruyut/TaiwanCalendar/data/{0}.json";

    /// <summary>查詢指定年份所有日曆</summary>
    public async Task<IActionResult> GetByYearAsync(HttpRequest req)
    {
        if (!int.TryParse(req.Query["year"], out var year) || year < 2000 || year > 2100)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份參數（2000-2100）。"));

        var days = await reader.GetByYearAsync(year);
        return new OkObjectResult(ApiResponse.Ok(days));
    }

    /// <summary>從政府 API 匯入指定年份的行事曆資料</summary>
    public async Task<IActionResult> ImportYearAsync(HttpRequest req)
    {
        if (!int.TryParse(req.Query["year"], out var year) || year < 2000 || year > 2100)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份參數（2000-2100）。"));

        // 從外部 API 取得資料
        var url = string.Format(CalendarApiUrl, year);
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (Exception ex)
        {
            return new ObjectResult(ApiResponse.Fail($"無法連線至行事曆 API：{ex.Message}"))
                { StatusCode = 502 };
        }

        if (!response.IsSuccessStatusCode)
            return new ObjectResult(ApiResponse.Fail($"行事曆 API 回傳錯誤：{(int)response.StatusCode}"))
                { StatusCode = 502 };

        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<TaiwanCalendarEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (items is null || items.Count == 0)
            return new BadRequestObjectResult(ApiResponse.Fail($"找不到 {year} 年的行事曆資料。"));

        // 刪除該年度舊資料
        var existing = await db.CalendarDays.Where(c => c.Year == year).ToListAsync();
        db.CalendarDays.RemoveRange(existing);

        // 寫入新資料
        var entities = items.Select(entry =>
        {
            var date = DateTime.ParseExact(entry.Date, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture);
            return new CalendarDay
            {
                Date        = date,
                IsHoliday   = entry.IsHoliday,
                Description = entry.Description ?? "",
                Year        = year,
            };
        }).ToList();

        db.CalendarDays.AddRange(entities);
        await db.SaveChangesAsync();

        var holidayCount = entities.Count(e => e.IsHoliday);
        return new OkObjectResult(ApiResponse.Ok(new
        {
            Year = year,
            TotalDays = entities.Count,
            Holidays = holidayCount,
            Workdays = entities.Count - holidayCount,
        }, $"成功匯入 {year} 年行事曆，共 {entities.Count} 天（假日 {holidayCount} 天）。"));
    }

    /// <summary>手動新增單筆行事曆</summary>
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateCalendarDayRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        // 檢查是否重複
        var exists = await db.CalendarDays.AnyAsync(c => c.Date == body.Date.Date);
        if (exists)
            return new BadRequestObjectResult(ApiResponse.Fail($"日期 {body.Date:yyyy-MM-dd} 已存在。"));

        var entity = new CalendarDay
        {
            Date        = body.Date.Date,
            IsHoliday   = body.IsHoliday,
            Description = body.Description,
            Year        = body.Date.Year,
        };

        db.CalendarDays.Add(entity);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new CalendarDayDto(
            entity.Id, entity.Date, entity.IsHoliday, entity.Description, entity.Year)));
    }

    /// <summary>手動更新單筆行事曆</summary>
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var entity = await db.CalendarDays.FindAsync(intId);
        if (entity is null)
            return new NotFoundObjectResult(ApiResponse.Fail("找不到該行事曆資料。"));

        var body = await req.ReadFromJsonAsync<UpdateCalendarDayRequest>();
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        if (body.IsHoliday.HasValue) entity.IsHoliday = body.IsHoliday.Value;
        if (body.Description is not null) entity.Description = body.Description;

        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(new CalendarDayDto(
            entity.Id, entity.Date, entity.IsHoliday, entity.Description, entity.Year)));
    }

    /// <summary>刪除單筆行事曆</summary>
    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var entity = await db.CalendarDays.FindAsync(intId);
        if (entity is null)
            return new NotFoundObjectResult(ApiResponse.Fail("找不到該行事曆資料。"));

        db.CalendarDays.Remove(entity);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok<object?>(null, "已刪除。"));
    }

    /// <summary>政府 API 回傳的行事曆項目</summary>
    private record TaiwanCalendarEntry(string Date, string Week, bool IsHoliday, string? Description);
}
