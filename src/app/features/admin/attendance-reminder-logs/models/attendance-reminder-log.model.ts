/** 打卡提醒推播紀錄。 */
export interface AttendanceReminderLog {
  id: number;
  batchId: string;
  tickedAt: string;            // UTC ISO
  tickedAtTaipei: string;      // 台北時區 ISO
  targetTimeTaipei: string;    // "09:00"
  reminderType: ReminderType;
  triggerSource: TriggerSource;
  triggeredByUserId?: string | null;
  triggeredByName?: string | null;
  userId?: string | null;
  userName?: string | null;
  lineUserIdSnapshot?: string | null;
  userNameSnapshot?: string | null;
  status: LogStatus;
  errorCategory?: ErrorCategory | null;
  errorMessage?: string | null;
  httpStatusCode?: number | null;
  durationMs?: number | null;
  createdAt: string;
}

/** 列表頁統計卡資料。 */
export interface AttendanceReminderLogStats {
  todayPushed: number;
  todayFailed: number;
  todayBatchTicks: number;
  last7Days: AttendanceReminderLogDaily[];
}

export interface AttendanceReminderLogDaily {
  day: string;
  pushed: number;
  failed: number;
}

export type ReminderType  = 'clockIn' | 'clockOut' | 'batchStart';
export type TriggerSource = 'auto' | 'manual';
export type LogStatus     = 'success' | 'failure' | 'batchStart';
export type ErrorCategory = 'not_friend' | 'token_invalid' | 'rate_limited' | 'network_error' | 'unknown' | 'system_error';

export const REMINDER_TYPE_LABELS: Record<ReminderType, string> = {
  clockIn:    '上班提醒',
  clockOut:   '下班提醒',
  batchStart: '批次啟動',
};

export const STATUS_LABELS: Record<LogStatus, string> = {
  success:    '成功',
  failure:    '失敗',
  batchStart: '批次',
};

export const TRIGGER_SOURCE_LABELS: Record<TriggerSource, string> = {
  auto:   '自動排程',
  manual: '手動觸發',
};

export const ERROR_CATEGORY_LABELS: Record<ErrorCategory, string> = {
  not_friend:    '未加好友',
  token_invalid: 'Token 失效',
  rate_limited:  '速率限制 (429)',
  network_error: '網路錯誤',
  unknown:       '其他錯誤',
  system_error:  '系統例外',
};

export interface AttendanceReminderLogQuery {
  from?: string;
  to?: string;
  reminderType?: ReminderType | '';
  status?: LogStatus | '';
  errorCategory?: ErrorCategory | '';
  userId?: string;
  triggerSource?: TriggerSource | '';
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
