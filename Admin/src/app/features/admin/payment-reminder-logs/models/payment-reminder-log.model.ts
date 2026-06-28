export interface PaymentReminderLog {
  id: number;
  batchId: string;
  tickedAt: string;
  tickedAtTaipei: string;
  reminderDateTaipei: string;
  triggerSource: 'auto' | 'manual';
  triggeredByUserName?: string;
  financeUserId?: string;
  financeUserName?: string;
  userNameSnapshot?: string;
  lineUserIdSnapshot?: string;
  itemCount: number;
  status: 'success' | 'failure' | 'batchStart' | 'skipped_already_sent';
  errorCategory?: string;
  errorMessage?: string;
  httpStatusCode?: number;
  durationMs?: number;
  createdAt: string;
}

export interface PaymentReminderLogPagedResult {
  items: PaymentReminderLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PaymentReminderRunResult {
  batchId: string;
  upcomingItemCount: number;
  financeUserCount: number;
  successCount: number;
  skippedAlreadySent: number;
  failureCount: number;
}

export const STATUS_LABELS: Record<string, string> = {
  success:              '已送達',
  failure:              '失敗',
  batchStart:           '批次開始',
  skipped_already_sent: '同日已推、跳過',
};

export const STATUS_CLASSES: Record<string, string> = {
  success:              'bg-success-subtle text-success',
  failure:              'bg-danger-subtle text-danger',
  batchStart:           'bg-secondary-subtle text-secondary',
  skipped_already_sent: 'bg-warning-subtle text-warning-emphasis',
};
