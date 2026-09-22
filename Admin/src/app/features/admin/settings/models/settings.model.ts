export interface SystemSettings {
  // 站台設定
  siteName: string;
  siteUrl: string;
  contactEmail: string;
  siteDescription: string;
  language: string;
  timezone: string;
  sessionTimeoutMinutes: number;
  allowRegistration: boolean;
  requireEmailVerification: boolean;
  maintenanceMode: boolean;
  maintenanceMessage: string;
  // 工時設定
  workStartTime: string;        // HH:MM，例如 "09:00"
  workEndTime: string;          // HH:MM，例如 "18:00"
  monthlyOvertimeLimit: number; // 小時
  // 通知設定
  approvalEmailEnabled: boolean; // 是否寄送簽核流程相關 Email
  approvalLineEnabled: boolean;  // 是否推播簽核流程相關 LINE 訊息
  // 撥款提醒
  paymentReminderDaysBefore: number; // 預計撥款日提前提醒天數（0-30，預設 3）
  /**
   * 四週彈性工時切換日（ISO 字串）；null ＝ 尚未切換，全系統維持現行 08:00–17:00 制。
   * 判定基準是**該筆資料自己的日期**，故切換後的歷史資料仍走舊制。
   */
  flexibleWorkStartDate: string | null;
}
