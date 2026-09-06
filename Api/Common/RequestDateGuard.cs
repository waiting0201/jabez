namespace Jabez.Api.Common;

/// <summary>
/// 申請單「使用者自行輸入的日期」合理範圍檢查（單一真相，純函式無 I/O，比照 <see cref="OvertimePayCalculator"/>）。
///
/// <para>
/// 存在理由：<c>&lt;input type="date"&gt;</c> 的年份欄可以打任意 4 位數，使用者把民國年當西元年輸入
/// （115 → 西元 0115 年）時，前後端都沒有任何一關會擋下來。2026-09 實際發生過一次：
/// 一張加班單的 <c>OvertimeDate</c> 存成 <c>0115-09-06</c>，打卡頁以
/// <c>CAST(OvertimeDate AS DATE) = 今日</c> 撈不到該單，員工當天完全無法打加班卡；
/// 而單子已 <c>approved</c>（只有 draft 能編輯），本人也改不掉，只能直接改 DB。
/// 順帶一提，iOS 的日期選擇器對 1582 年前的日期改用儒略曆呈現，同一筆資料在清單頁顯示 09-06、
/// 編輯頁卻顯示 9 月 7 日，讓誤植更難被辨認。
/// </para>
///
/// <para>
/// 範圍刻意寬鬆（今日 ±3 年）：目的只在攔截「差了 1911 年的民國年」與明顯誤植的年份，
/// 不是業務規則。真正的業務期間限制（請假重疊、出差起訖、發票期限…）留在各 Handler 既有的驗證，
/// 此處放行不代表該日期在業務上合法。範圍必須容納既有的合法情境：
/// 育嬰留職停薪最長 730 天（迄日可達今日 +2 年）、補請去年度的發票、跨年度的專案活動。
/// </para>
///
/// <para>
/// 前端對應：<c>Admin/src/app/shared/utils/date-bounds.ts</c>（同樣的 ±3 年 + 子女出生日期 3 年）
/// 以 <c>min</c> / <c>max</c> 屬性限制原生日期選擇器並擋下送出。兩處數字必須一起改。
/// </para>
/// </summary>
public static class RequestDateGuard
{
    /// <summary>可回溯年數（今日往前）</summary>
    public const int YearsBack = 3;

    /// <summary>可預填年數（今日往後）</summary>
    public const int YearsAhead = 3;

    /// <summary>子女出生日期可回溯年數（育嬰留停資格為「子女未滿 3 歲」，故與法定門檻同值）</summary>
    public const int ChildBirthYearsBack = 3;

    /// <summary>合理範圍下界（含）</summary>
    public static DateTime Min => Clock.Today.AddYears(-YearsBack);

    /// <summary>合理範圍上界（含）</summary>
    public static DateTime Max => Clock.Today.AddYears(YearsAhead);

    /// <summary>是否落在合理範圍外（null 一律視為合法，必填與否由呼叫端自行驗證）</summary>
    public static bool IsOutOfRange(DateTime? value) =>
        value is { } v && (v.Date < Min || v.Date > Max);

    /// <summary>
    /// 單一日期欄位檢查，超出合理範圍丟 400。
    /// </summary>
    /// <param name="value">待檢日期；null 直接放行</param>
    /// <param name="fieldLabel">錯誤訊息中的欄位中文名（如「加班日期」）</param>
    public static void Ensure(DateTime? value, string fieldLabel)
    {
        if (IsOutOfRange(value))
            throw AppException.BadRequest(Message(fieldLabel, value!.Value, Min, Max));
    }

    /// <summary>
    /// 多個日期欄位一次檢查（依序，第一個不合理者即丟 400）。
    /// </summary>
    public static void EnsureAll(params (DateTime? Value, string Label)[] fields)
    {
        foreach (var (value, label) in fields)
            Ensure(value, label);
    }

    /// <summary>
    /// 明細列表中同一個日期欄位的整批檢查（如各列的發票日期）。
    /// </summary>
    public static void EnsureEach<T>(IEnumerable<T>? items, Func<T, DateTime?> selector, string fieldLabel)
    {
        if (items is null) return;
        foreach (var item in items)
            Ensure(selector(item), fieldLabel);
    }

    /// <summary>
    /// 只允許「過去 N 年內且不得晚於今日」的日期（如育嬰留停的子女出生日期）。
    /// </summary>
    public static void EnsurePastWithin(DateTime? value, string fieldLabel, int yearsBack)
    {
        if (value is not { } v) return;

        var min = Clock.Today.AddYears(-yearsBack);
        var max = Clock.Today;
        if (v.Date < min || v.Date > max)
            throw AppException.BadRequest(Message(fieldLabel, v, min, max));
    }

    private static string Message(string fieldLabel, DateTime value, DateTime min, DateTime max) =>
        $"{fieldLabel}「{value:yyyy-MM-dd}」超出合理範圍（{min:yyyy-MM-dd} ~ {max:yyyy-MM-dd}）。" +
        "請確認年份是否誤填民國年（例：民國 115 年應輸入西元 2026 年）。";
}
