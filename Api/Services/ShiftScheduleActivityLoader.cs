using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 排班月曆上「某月每天有哪些活動日」的共用載入（static、唯讀）。
///
/// 消費點：個人排班月曆（<c>ShiftScheduleHandler</c>）與改班表單／簽核詳情（<c>ShiftChangeRequestService</c>），
/// 兩處原本各寫一份幾乎相同的查詢，且同一天多個活動只取一筆。
///
/// 範圍為**全公司**活動日（不依部門過濾）—— 同仁排班時要看得到別部門的活動。
/// 同一天的排序：本人被指派者在前，其餘依 Id，故 <c>First()</c> 即「與本人最相關」的那一筆。
/// </summary>
public static class ShiftScheduleActivityLoader
{
    public static async Task<ILookup<DateTime, ShiftScheduleActivityDto>> LoadAsync(
        AppDbContext db, Guid userId, DateTime monthStart, DateTime monthEnd)
    {
        // ⚠ 參與人員走導覽屬性而非 .Join(db.Users, …)：後者會讓 EF 把 join key 推斷成 object 而無法翻譯
        //（見 ActivityDayHandler.ToDtoAsync）。建立者沒有導覽屬性，以相關子查詢取名。
        var rows = await db.ActivityDays.AsNoTracking()
            .Where(a => a.Date >= monthStart && a.Date <= monthEnd)
            .Select(a => new
            {
                a.Id,
                a.Date,
                a.Title,
                DepartmentName = a.Department != null ? a.Department.Name : null,
                CreatedByName  = db.Users.Where(u => u.Id == a.CreatedByUserId).Select(u => u.Name).FirstOrDefault(),
                IsAssignee     = a.Assignees.Any(x => x.UserId == userId),
                AssigneeNames  = a.Assignees
                    .Where(x => x.User != null)
                    .OrderBy(x => x.User!.Name)
                    .Select(x => x.User!.Name)
                    .ToList(),
            })
            .ToListAsync();

        return rows
            .OrderByDescending(a => a.IsAssignee).ThenBy(a => a.Id)
            .ToLookup(
                a => a.Date.Date,
                a => new ShiftScheduleActivityDto(
                    a.Id, a.Title, a.DepartmentName, a.CreatedByName, a.IsAssignee, [.. a.AssigneeNames]));
    }
}
