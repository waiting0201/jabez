/**
 * 報表時段篩選工具：將 日 / 週 / 月 模式下的使用者輸入轉成 { dateFrom, dateTo }（皆為 'YYYY-MM-DD'）。
 * 三種模式皆 inclusive；後端據此過濾日期區間。
 */

export type FilterMode = 'day' | 'week' | 'month';

export interface DateRange {
  dateFrom: string;
  dateTo: string;
}

/** 'YYYY-MM-DD' 格式化（避免 toISOString 的 UTC 漂移） */
function fmt(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${dd}`;
}

/**
 * 解析 input[type=week] 回傳的 ISO 8601 週字串（格式：'YYYY-Www'）
 * 演算法：每年的 1 月 4 日必落在 ISO Week 1，找該週的星期一作為基準，
 *        再加 (week - 1) * 7 天得到目標週的星期一。
 * 範例：'2026-W18' → { monday: '2026-04-27', sunday: '2026-05-03' }
 */
export function isoWeekToRange(weekStr: string): DateRange | null {
  const match = /^(\d{4})-W(\d{2})$/.exec(weekStr);
  if (!match) return null;

  const year = Number(match[1]);
  const week = Number(match[2]);
  if (week < 1 || week > 53) return null;

  // Jan 4 必在 ISO Week 1
  const jan4 = new Date(year, 0, 4);
  // ISO 中 Monday=1..Sunday=7；JS getDay 中 Sunday=0
  const jan4Dow = jan4.getDay() === 0 ? 7 : jan4.getDay();
  const week1Monday = new Date(year, 0, 4 - (jan4Dow - 1));

  const monday = new Date(week1Monday);
  monday.setDate(week1Monday.getDate() + (week - 1) * 7);

  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);

  return { dateFrom: fmt(monday), dateTo: fmt(sunday) };
}

/** 月模式：年 + 月 → 月初 / 月底（new Date(y, m, 0) 為前月最後一天，故傳入 month 為 1-12 取當月底） */
export function monthToRange(year: number, month: number): DateRange {
  const first = new Date(year, month - 1, 1);
  const last = new Date(year, month, 0); // 下月 day 0 = 當月最後一天
  return { dateFrom: fmt(first), dateTo: fmt(last) };
}

/** 日模式：dateFrom = dateTo */
export function dayToRange(dateStr: string): DateRange | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(dateStr)) return null;
  return { dateFrom: dateStr, dateTo: dateStr };
}

/** 取得本週的 ISO 週字串（'YYYY-Www'），用於進頁時預設值 */
export function currentIsoWeek(d: Date = new Date()): string {
  const target = new Date(d);
  // 推進到本週四（ISO 週號以週四所屬年份為準）
  const dow = target.getDay() === 0 ? 7 : target.getDay();
  target.setDate(target.getDate() + 4 - dow);
  const year = target.getFullYear();
  const yearStart = new Date(year, 0, 1);
  const week = Math.ceil(((target.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
  return `${year}-W${String(week).padStart(2, '0')}`;
}
