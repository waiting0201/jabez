using System.IdentityModel.Tokens.Jwt;
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
                Description = MapDescription(entry.Description),
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

    // ── 加倍工資日（選舉投票日等）─────────────────────────────────────────
    // 不改任何人的日別，只在薪資上對當天有上班打卡者加發 1 日日薪（PayrollReadService 併入國定假日出勤一行）。

    /// <summary>查詢指定年份的加倍工資日</summary>
    public async Task<IActionResult> GetDoublePayDaysAsync(HttpRequest req)
    {
        if (!int.TryParse(req.Query["year"], out var year) || year < 2000 || year > 2100)
            return new BadRequestObjectResult(ApiResponse.Fail("請提供有效的年份參數（2000-2100）。"));

        var days = await reader.GetDoublePayDaysByYearAsync(year);
        return new OkObjectResult(ApiResponse.Ok(days));
    }

    /// <summary>新增加倍工資日</summary>
    public async Task<IActionResult> CreateDoublePayDayAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateDoublePayDayRequest>();
        if (body is null || body.Date == default)
            return new BadRequestObjectResult(ApiResponse.Fail("請選擇日期。"));

        var name = body.Name?.Trim() ?? "";
        if (name.Length == 0)
            return new BadRequestObjectResult(ApiResponse.Fail("請填寫名稱。"));
        if (name.Length > 50)
            return new BadRequestObjectResult(ApiResponse.Fail("名稱最多 50 字。"));

        RequestDateGuard.Ensure(body.Date, "日期");
        var date = body.Date.Date;

        if (await db.DoublePayDays.AnyAsync(d => d.Date == date))
            return new BadRequestObjectResult(ApiResponse.Fail($"{date:yyyy-MM-dd} 已設為加倍工資日。"));

        // 國定假日本來就有「國定假日出勤加倍工資」，同一天再設會讓兩套規則打架（國定假日蓋過個人排班）
        var calendar = await db.CalendarDays.AsNoTracking().FirstOrDefaultAsync(c => c.Date == date);
        if (calendar is not null && PublicHolidayRule.IsPublicHoliday(calendar.IsHoliday, calendar.Description))
            return new BadRequestObjectResult(ApiResponse.Fail(
                $"{date:yyyy-MM-dd} 已是國定假日（{calendar.Description}），出勤本來就會加倍計薪，不需再設加倍工資日。"));

        var sub = req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var entity = new DoublePayDay
        {
            Date        = date,
            Name        = name,
            CreatedAt   = Clock.Now,
            CreatedById = Guid.TryParse(sub, out var uid) ? uid : null,
        };
        db.DoublePayDays.Add(entity);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(
            new DoublePayDayDto(entity.Id, entity.Date, entity.Name), "已新增加倍工資日。"));
    }

    /// <summary>刪除加倍工資日（薪資即時重算、無月結快照，刪除後該月加倍工資即消失）</summary>
    public async Task<IActionResult> DeleteDoublePayDayAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid ID format."));

        var entity = await db.DoublePayDays.FindAsync(intId);
        if (entity is null)
            return new NotFoundObjectResult(ApiResponse.Fail("找不到該加倍工資日。"));

        db.DoublePayDays.Remove(entity);
        await db.SaveChangesAsync();

        var message = entity.Date < Clock.Today
            ? "已刪除。該日已過，次月薪資的加倍工資會跟著移除，若已發薪請另行處理。"
            : "已刪除。";
        return new OkObjectResult(ApiResponse.Ok<object?>(null, message));
    }

    /// <summary>
    /// 外部行事曆的假日名稱 → 本公司用語。目前只有一條：「補假」→「彈性休假日」。
    ///
    /// ⚠ **改名必須落在這裡**，不能只改 DB 或畫面：ImportYearAsync 是「整年 RemoveRange 後重建」，
    /// 只要有人再按一次「匯入 {年} 年」，手改過的名稱就會整批被沖回「補假」。
    /// 既有年度的資料另以一次性腳本 Data/Scripts/15 回填。
    /// </summary>
    private static string MapDescription(string? source) =>
        source == CalendarDescriptions.SourceMakeupHoliday
            ? CalendarDescriptions.FlexibleHoliday
            : source ?? "";

    /// <summary>政府 API 回傳的行事曆項目</summary>
    private record TaiwanCalendarEntry(string Date, string Week, bool IsHoliday, string? Description);
}
