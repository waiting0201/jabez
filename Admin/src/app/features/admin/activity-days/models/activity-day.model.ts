/**
 * 活動日（四週彈性工時 §3.2）
 *
 * ⚠ 這是**疊加在日別之上的旗標**，不是第 5 種日別 ——
 * 同一天可以既是「上班日」又是「活動日」，也可以壓在「國定假日」上。
 */
export interface ActivityDayAssignee {
  userId: string;
  userName: string;
  departmentName: string | null;
  jobTitleName: string | null;
}

export interface ActivityDay {
  id: number;
  date: string;
  departmentId: number;
  departmentName: string | null;
  title: string;
  createdByUserId: string;
  createdByName: string | null;
  /** 該活動日是否落在國定假日上（此時預定人力當天可直接打上下班卡、不需加班單） */
  isPublicHoliday: boolean;
  holidayName: string | null;
  assignees: ActivityDayAssignee[];
}

export interface SaveActivityDayRequest {
  date: string;
  departmentId: number;
  title: string;
  assigneeUserIds: string[];
}

/** 改期後排班檢核不通過的同仁。系統**不會**自動改寫他們的班表，需由其自行送〈改班申請〉。 */
export interface AffectedSchedule {
  userId: string;
  userName: string;
  year: number;
  month: number;
  blocks: string[];
}

export interface SaveActivityDayResult {
  activityDay: ActivityDay;
  dateChanged: boolean;
  affected: AffectedSchedule[];
}
