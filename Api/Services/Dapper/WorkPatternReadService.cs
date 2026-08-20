using System.Data;
using Dapper;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 員工出勤型態查詢 —— 目前只服務「是否為排班制」一個問題。
///
/// 排班制（賣店 / 營業所）員工的六日與國定假日皆為工作日，
/// 此旗標會傳入 <see cref="Jabez.Api.Common.WorkCalendarHelper"/> /
/// <see cref="Jabez.Api.Common.LeaveDayExpander"/> 決定是否扣除假日。
///
/// 消費點一律以「假單所有人 / 打卡本人」的 userId 查詢，**不可用呼叫者 id**：
/// Superadmin 可代他人送請假單、主管核准銷假時呼叫者也不是本人，用錯 id 會靜默算錯天數。
/// </summary>
public interface IWorkPatternReadService
{
    /// <summary>該員工是否為排班制（六日與國定假日視為工作日）。查無此人回 false。</summary>
    Task<bool> IsShiftWorkerAsync(Guid userId);
}

/// <summary>
/// 以記憶體 memo 收斂同一請求內的重複查詢（一張請假單的 Create / Update / Submit 會問好幾次）。
/// memo 生命週期＝單次 HTTP 請求（Scoped），故寫入路徑（UserHandler 改完旗標後）不得沿用同一實例判斷。
/// </summary>
public sealed class WorkPatternReadService(IDbConnection db) : IWorkPatternReadService
{
    private readonly Dictionary<Guid, bool> _memo = [];

    public async Task<bool> IsShiftWorkerAsync(Guid userId)
    {
        if (_memo.TryGetValue(userId, out var cached)) return cached;

        const string sql = "SELECT IsShiftWorker FROM Users WHERE Id = @UserId";
        var value = await db.ExecuteScalarAsync<bool?>(sql, new { UserId = userId }) ?? false;

        _memo[userId] = value;
        return value;
    }
}
