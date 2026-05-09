export interface LineQuota {
  /** 'limited' = 有月度上限；'none' = 無上限方案 */
  type: 'limited' | 'none';
  /** 月度上限；type='none' 時為 null */
  limit: number | null;
  /** 本月已使用則數 */
  used: number;
  /** 剩餘可發送則數；type='none' 時為 null */
  remaining: number | null;
}
