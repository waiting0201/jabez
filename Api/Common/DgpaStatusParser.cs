using System.Net;
using System.Text.RegularExpressions;

namespace Jabez.Api.Common;

/// <summary>公告解析出的單一指令種類。</summary>
public enum DgpaDirectiveKind
{
    /// <summary>全天停止上班</summary>
    Full,
    /// <summary>上午停止上班</summary>
    Am,
    /// <summary>下午停止上班</summary>
    Pm,
    /// <summary>照常上班，或僅停止上課（不影響上班）</summary>
    None,
    /// <summary>只限部分地區 / 鄉鎮（不自動套用，交 HR 判斷）</summary>
    Partial,
    /// <summary>無法辨識的句型（不自動套用，交 HR 判斷）</summary>
    Unknown,
}

/// <summary>某縣市對某一天的公告解讀。</summary>
/// <param name="DayOffset">0＝今天、1＝明天、2＝後天</param>
/// <param name="Text">該子句原文（供 HR 核對）</param>
public sealed record DgpaDirective(int DayOffset, DgpaDirectiveKind Kind, string Text);

/// <summary>單一縣市列的解析結果。</summary>
public sealed record DgpaCountyStatus(string Code, string CountyName, string RawText, IReadOnlyList<DgpaDirective> Directives);

/// <summary>整頁解析結果。</summary>
public sealed record DgpaPage(DateTime PageDate, DateTime? SourceUpdatedAt, IReadOnlyList<DgpaCountyStatus> Counties);

/// <summary>
/// 行政院人事行政總處「天然災害停止上班及上課情形」頁面
/// （https://www.dgpa.gov.tw/typh/daily/nds.html）的純函式解析器 —— 無 I/O，可獨立測試。
///
/// 頁面結構（2026-10-23 實測）：
///   · 標題 <c>&lt;div class="Header_YMD"&gt;115年 10月 23日 天然災害停止上班及上課情形&lt;/div&gt;</c>（民國年，分行排列）
///   · <c>更新時間：2026/10/23 13:02:04</c>
///   · <c>&lt;TABLE id="Table"&gt;</c>：每個縣市一列 <c>&lt;TR&gt;</c>，第一個區域欄有 rowspan（只在每區第一列出現），
///     所以一律取該列「最後兩個 TD」＝縣市名稱、狀態文字；表頭列（縣市名稱）與備註列（單一 colspan TD）自然被略過。
///   · 狀態文字例：「今天停止上班、停止上課。」「今天照常上班、照常上課。」
///
/// 解讀原則 —— <b>寧可漏套交人工，不可誤套</b>：
///   · 「今天 / 明天 / 後天」＋ 句子切分；一個縣市可能同時有多個日期的句子。
///   · 停止上班 → 全天；「上午 … 停止上班」→ am；「下午 … 停止上班」→ pm（可含「晚上」）。
///   · 照常上班、或只有「停止上課」→ None。
///   · 子句含鄉 / 鎮 / 區 / 市區 / 村 / 里 或「部分」等字樣 → Partial（不套用、通知 HR）。
///   · 只有晚上（夜間）停班、同子句又停又照常、缺日期詞等 → Unknown（不套用、通知 HR）。
/// </summary>
public static class DgpaStatusParser
{
    private static readonly Regex RowRegex   = new(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CellRegex  = new(@"<td\b[^>]*>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagRegex   = new(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HeaderYmd  = new(@"(\d{2,3})\s*年\s*(\d{1,2})\s*月\s*(\d{1,2})\s*日", RegexOptions.Compiled);
    private static readonly Regex UpdatedAt  = new(@"更新時間[:：]\s*(\d{4})/(\d{1,2})/(\d{1,2})\s+(\d{1,2}):(\d{2}):(\d{2})", RegexOptions.Compiled);
    private static readonly Regex DayWord    = new(@"今天|今日|明天|明日|後天", RegexOptions.Compiled);
    private static readonly Regex PeriodWord = new(@"上午|下午|中午|晚上|晚間|夜間|夜晚|傍晚", RegexOptions.Compiled);
    // 「地區」「區域」亦含「區」→ 同樣視為部分區域（寧可交人工）
    private static readonly Regex PartialWord = new(@"[一-鿿]{1,4}(鄉|鎮|區|村|里)|部分|偏遠|山區|沿海", RegexOptions.Compiled);

    /// <summary>解析整頁 HTML；標題日期無法取得時退回更新時間的日期，兩者皆缺則回 null。</summary>
    public static DgpaPage? ParsePage(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        DateTime? updated = null;
        var um = UpdatedAt.Match(html);
        if (um.Success)
            updated = new DateTime(int.Parse(um.Groups[1].Value), int.Parse(um.Groups[2].Value), int.Parse(um.Groups[3].Value),
                                   int.Parse(um.Groups[4].Value), int.Parse(um.Groups[5].Value), int.Parse(um.Groups[6].Value));

        DateTime? pageDate = null;
        // 標題只在 Header_YMD 區塊內；限縮範圍避免誤抓頁面其他「N年N月N日」
        var hdrIdx = html.IndexOf("Header_YMD", StringComparison.OrdinalIgnoreCase);
        if (hdrIdx >= 0)
        {
            var seg = html.Substring(hdrIdx, Math.Min(400, html.Length - hdrIdx));
            var hm  = HeaderYmd.Match(StripTags(seg));
            if (hm.Success)
            {
                var y = int.Parse(hm.Groups[1].Value);
                if (y < 1911) y += 1911;                 // 民國年
                try { pageDate = new DateTime(y, int.Parse(hm.Groups[2].Value), int.Parse(hm.Groups[3].Value)); }
                catch (ArgumentOutOfRangeException) { pageDate = null; }
            }
        }
        pageDate ??= updated?.Date;
        if (pageDate is null) return null;

        var counties = new List<DgpaCountyStatus>();
        var tableIdx = html.IndexOf("<TABLE", StringComparison.OrdinalIgnoreCase);
        var body = tableIdx >= 0 ? html[tableIdx..] : html;

        foreach (Match row in RowRegex.Matches(body))
        {
            var cells = CellRegex.Matches(row.Groups[1].Value).Select(m => StripTags(m.Groups[1].Value)).ToList();
            if (cells.Count < 2) continue;                    // 備註列（單一 colspan TD）
            var name = cells[^2];
            var code = TaiwanCounties.CodeOfName(name);
            if (code is null) continue;                       // 表頭列 / 非縣市列
            var text = cells[^1];
            counties.Add(new DgpaCountyStatus(code, TaiwanCounties.NameOf(code), text, ParseStatusText(text)));
        }

        return new DgpaPage(pageDate.Value.Date, updated, counties);
    }

    /// <summary>解析單一縣市的狀態文字（可能含多個日期的多個句子）。</summary>
    public static IReadOnlyList<DgpaDirective> ParseStatusText(string raw)
    {
        var text = Normalize(raw);
        if (text.Length == 0) return [new DgpaDirective(0, DgpaDirectiveKind.Unknown, raw.Trim())];

        // 以日期詞為界切成子句：「今天…。明天…」→ [今天…。, 明天…]
        var matches = DayWord.Matches(text);
        if (matches.Count == 0)
            return [new DgpaDirective(0, DgpaDirectiveKind.Unknown, text)];

        var result = new List<DgpaDirective>();
        // 日期詞之前若還有文字（缺日期詞的前導句）→ 無法歸屬，交人工
        if (matches[0].Index > 0 && text[..matches[0].Index].Trim('。', '，', ',', '；', ';', ' ').Length > 0)
            result.Add(new DgpaDirective(0, DgpaDirectiveKind.Unknown, text[..matches[0].Index]));

        for (int i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end   = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var clause = text[start..end].Trim('。', '，', ',', '；', ';', ' ');
            var offset = matches[i].Value switch { "明天" or "明日" => 1, "後天" => 2, _ => 0 };
            result.Add(new DgpaDirective(offset, Classify(clause), clause));
        }

        // 同一天出現多個子句（例：「今天停止上班。今天…」）→ 取最保守者：只要有 Partial/Unknown 就不套用
        return result
            .GroupBy(d => d.DayOffset)
            .Select(g => g.Count() == 1 ? g.First() : Merge(g.ToList()))
            .OrderBy(d => d.DayOffset)
            .ToList();
    }

    private static DgpaDirective Merge(List<DgpaDirective> items)
    {
        var bad = items.FirstOrDefault(d => d.Kind is DgpaDirectiveKind.Partial or DgpaDirectiveKind.Unknown);
        var text = string.Join("。", items.Select(i => i.Text));
        if (bad is not null) return new DgpaDirective(items[0].DayOffset, bad.Kind, text);
        // 都是可套用的：不同時段合併（am + pm → full）；其餘（含 None 與停班並存）視為無法辨識
        var kinds = items.Select(i => i.Kind).Distinct().ToList();
        if (kinds.Count == 1) return new DgpaDirective(items[0].DayOffset, kinds[0], text);
        if (kinds.Count == 2 && kinds.Contains(DgpaDirectiveKind.Am) && kinds.Contains(DgpaDirectiveKind.Pm))
            return new DgpaDirective(items[0].DayOffset, DgpaDirectiveKind.Full, text);
        return new DgpaDirective(items[0].DayOffset, DgpaDirectiveKind.Unknown, text);
    }

    /// <summary>單一子句（已確定日期）→ 指令種類。</summary>
    private static DgpaDirectiveKind Classify(string clause)
    {
        // 只限部分地區 / 鄉鎮
        if (PartialWord.IsMatch(clause)) return DgpaDirectiveKind.Partial;

        // 依「上午 / 下午 / 晚上」切段，沒有動詞的段落沿用下一段的動詞（「下午及晚上停止上班」）
        var marks = PeriodWord.Matches(clause);
        if (marks.Count == 0)
        {
            return VerbOf(clause) switch
            {
                Verb.Stop   => DgpaDirectiveKind.Full,
                Verb.Normal => DgpaDirectiveKind.None,
                Verb.ClassOnly => DgpaDirectiveKind.None,
                Verb.Conflict => DgpaDirectiveKind.Unknown,
                _ => DgpaDirectiveKind.Unknown,
            };
        }

        // 第一個時段詞之前的文字（例「今天」）若本身有動詞 → 代表有「整天」層級的敘述，與分時段並存 → 交人工
        var head = clause[..marks[0].Index];
        if (VerbOf(head) is Verb.Stop or Verb.Normal) return DgpaDirectiveKind.Unknown;

        bool amStop = false, pmStop = false, nightStop = false, anyVerb = false;
        var pending = new List<string>();
        for (int i = 0; i < marks.Count; i++)
        {
            var s = marks[i].Index;
            var e = i + 1 < marks.Count ? marks[i + 1].Index : clause.Length;
            var seg = clause[s..e];
            pending.Add(marks[i].Value);

            var v = VerbOf(seg);
            if (v == Verb.None) continue;                // 無動詞 → 等下一段
            if (v == Verb.Conflict) return DgpaDirectiveKind.Unknown;
            anyVerb = true;
            if (v == Verb.Stop)
                foreach (var p in pending)
                {
                    if (p == "上午") amStop = true;
                    else if (p == "下午" || p == "中午" || p == "傍晚") pmStop = true;
                    else nightStop = true;
                }
            pending.Clear();
        }
        if (pending.Count > 0) return DgpaDirectiveKind.Unknown;     // 結尾的時段詞沒有動詞可套
        if (!anyVerb) return DgpaDirectiveKind.Unknown;

        if (amStop && pmStop) return DgpaDirectiveKind.Full;
        if (amStop) return DgpaDirectiveKind.Am;
        if (pmStop) return DgpaDirectiveKind.Pm;
        // 沒有任何日間時段停班：純晚間停班不影響日間出勤但也不是「照常」→ 交人工；其餘（全部照常）→ None
        return nightStop ? DgpaDirectiveKind.Unknown : DgpaDirectiveKind.None;
    }

    private enum Verb { None, Stop, Normal, ClassOnly, Conflict }

    private static Verb VerbOf(string s)
    {
        bool stopWork   = s.Contains("停止上班");
        bool normalWork = s.Contains("照常上班");
        if (stopWork && normalWork) return Verb.Conflict;
        if (stopWork) return Verb.Stop;
        if (normalWork) return Verb.Normal;
        // 只有課程相關敘述（停止上課 / 照常上課）→ 不影響上班
        if (s.Contains("停止上課") || s.Contains("照常上課")) return Verb.ClassOnly;
        return Verb.None;
    }

    private static string StripTags(string html) =>
        Normalize(WebUtility.HtmlDecode(TagRegex.Replace(html, " ")));

    /// <summary>去掉全形 / 半形空白與換行（子句比對不吃空白），並把全形標點統一。</summary>
    private static string Normalize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch) || ch == '　') continue;
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
