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
}
