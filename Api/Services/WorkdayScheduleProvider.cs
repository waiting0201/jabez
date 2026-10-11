using Jabez.Api.Common;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 「該日套用哪一套工作時段」的取用管道 —— 把 <c>SystemSetting.FlexibleWorkStartDate</c>
/// 讀成一個 per-request 快取，讓各處不必各自查 DB、也不必各自比日期。
///
/// 為什麼需要它：<see cref="WorkdayHours.For"/> 是純函式（好測、無 I/O），但切換日存在 DB。
/// 若讓每個消費點自己去讀 SystemSettings，會出現「有些地方讀了、有些地方忘了」的漂移 ——
/// 而漂移的症狀是**歷史月份的請假時段與應出勤時段悄悄變成新制**，畫面上看不出異常。
///
/// 生命週期為 Scoped：切換日在一次請求內不會變，查一次即可。
/// </summary>
public interface IWorkdayScheduleProvider
{
    /// <summary>取得該日期適用的時段。date 一律傳「資料自己的日期」（請假單 StartDate、打卡 RecordDate…），不是今天。</summary>
    Task<WorkdaySchedule> ForAsync(DateTime date);

    /// <summary>取得切換日本身（null ＝ 尚未切換）。需要一次解析整個區間時用，避免逐日 await。</summary>
    Task<DateTime?> GetSwitchDateAsync();
}

public sealed class WorkdayScheduleProvider(AppDbContext db) : IWorkdayScheduleProvider
{
    private bool      _loaded;
    private DateTime? _switchDate;

    public async Task<WorkdaySchedule> ForAsync(DateTime date) =>
        WorkdayHours.For(date, await GetSwitchDateAsync());

    public async Task<DateTime?> GetSwitchDateAsync()
    {
        if (_loaded) return _switchDate;

        _switchDate = await db.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => s.FlexibleWorkStartDate)
            .FirstOrDefaultAsync();
        _loaded = true;

        return _switchDate;
    }
}
