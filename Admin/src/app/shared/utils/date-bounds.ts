/**
 * 申請表單日期欄位的合理範圍（單一真相）。
 *
 * 存在理由：`<input type="date">` 的年份欄可以打任意 4 位數，把民國年當西元年輸入
 * （115 → 西元 0115 年）時沒有任何一關會擋。2026-09 實際發生過：一張加班單的加班日期
 * 存成 `0115-09-06`，打卡頁以「加班日期 = 今日」撈單撈不到，員工當天完全無法打加班卡；
 * 單子已核准（只有草稿能編輯），本人也改不掉，只能直接改 DB。
 * 該員工是在 iOS 的 LINE 內建瀏覽器操作，而 iOS 日期選擇器對 1582 年前的日期改用儒略曆呈現，
 * 同一筆資料清單頁顯示 09-06、編輯頁卻顯示 9 月 7 日，讓誤植更難被辨認。
 *
 * `min` / `max` 掛上去後，行動裝置的原生日期選擇器直接轉不到範圍外的年份（本案的根治點）；
 * 桌機瀏覽器仍可硬打，故後端 `Api/Common/RequestDateGuard.cs` 以同一組數字回 400 作為權威守門，
 * 並由 `.form-control:out-of-range` 的紅框樣式即時提示（見 docs/frontend-design.md §6）。
 *
 * 範圍刻意寬鬆（今日 ±3 年）：目的只在攔截「差了 1911 年的民國年」與明顯誤植的年份，不是業務規則。
 * 必須容納既有合法情境：育嬰留職停薪最長 730 天（迄日可達今日 +2 年）、補請去年度的發票、跨年度活動。
 * 前後端兩處數字必須一起改。
 */

/** 可回溯年數（今日往前） */
export const DATE_BOUND_YEARS_BACK = 3;

/** 可預填年數（今日往後） */
export const DATE_BOUND_YEARS_AHEAD = 3;

/** 子女出生日期可回溯年數（育嬰留停資格為「子女未滿 3 歲」，故與法定門檻同值） */
export const CHILD_BIRTH_YEARS_BACK = 3;

/** Date → `yyyy-MM-dd`（不經 toISOString，避免 UTC 位移，見 docs/frontend-design.md §6） */
const toDateInputValue = (d: Date): string =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

/** 今日 ± N 年的 `yyyy-MM-dd`（2/29 落在平年時由 Date 自動進位到 3/1，不影響防呆用途） */
const shiftYears = (years: number): string => {
  const d = new Date();
  d.setFullYear(d.getFullYear() + years);
  return toDateInputValue(d);
};

/** 申請單日期欄位下界（`<input type="date">` 的 min） */
export const MIN_REQUEST_DATE = shiftYears(-DATE_BOUND_YEARS_BACK);

/** 申請單日期欄位上界（`<input type="date">` 的 max） */
export const MAX_REQUEST_DATE = shiftYears(DATE_BOUND_YEARS_AHEAD);

/** 子女出生日期下界 */
export const MIN_CHILD_BIRTH_DATE = shiftYears(-CHILD_BIRTH_YEARS_BACK);

/** 子女出生日期上界（不得晚於今日） */
export const MAX_CHILD_BIRTH_DATE = toDateInputValue(new Date());
