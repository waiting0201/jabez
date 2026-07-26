import {Routes} from '@angular/router';
import {permissionGuard} from '@core/auth/guards/permission.guard';
import {UserList} from './users/pages/user-list/user-list';
import {UserForm} from './users/pages/user-form/user-form';
import {RoleList} from './roles/pages/role-list/role-list';
import {RoleForm} from './roles/pages/role-form/role-form';
import {PermissionList} from './permissions/pages/permission-list/permission-list';
import {PermissionForm} from './permissions/pages/permission-form/permission-form';
import {Settings} from './settings/pages/settings/settings';
import {DepartmentList} from './departments/pages/department-list/department-list';
import {DepartmentForm} from './departments/pages/department-form/department-form';
import {JobTitleList} from './job-titles/pages/job-title-list/job-title-list';
import {JobTitleForm} from './job-titles/pages/job-title-form/job-title-form';
import {VendorList} from './vendors/pages/vendor-list/vendor-list';
import {VendorForm} from './vendors/pages/vendor-form/vendor-form';
import {ApprovalList} from './approvals/pages/approval-list/approval-list';
import {ApprovalFlow} from './approvals/pages/approval-flow/approval-flow';
import {ProjectList} from './projects/pages/project-list/project-list';
import {ProjectForm} from './projects/pages/project-form/project-form';
import {PaymentList} from './payment-requests/pages/payment-list/payment-list';
import {PaymentForm} from './payment-requests/pages/payment-form/payment-form';
import {ApprovalTaskList} from './approval-tasks/pages/approval-task-list/approval-task-list';
import {ApprovalTaskReview} from './approval-tasks/pages/approval-task-review/approval-task-review';
import {LeaveRequestList} from './leave-requests/pages/leave-request-list/leave-request-list';
import {LeaveRequestForm} from './leave-requests/pages/leave-request-form/leave-request-form';
import {TravelRequestList} from './travel-requests/pages/travel-request-list/travel-request-list';
import {TravelRequestForm} from './travel-requests/pages/travel-request-form/travel-request-form';
import {TravelDetail} from './travel-requests/pages/travel-detail/travel-detail';
import {OvertimeRequestList} from './overtime-requests/pages/overtime-request-list/overtime-request-list';
import {OvertimeRequestForm} from './overtime-requests/pages/overtime-request-form/overtime-request-form';
import {AttendanceReport} from './reports/pages/attendance-report/attendance-report';
import {OvertimeReport} from './reports/pages/overtime-report/overtime-report';
import {PaymentReport} from './reports/pages/payment-report/payment-report';
import {ProjectWaterLevel} from './reports/pages/project-water-level/project-water-level';
import {InsuranceBracketList} from './insurance-brackets/pages/insurance-bracket-list/insurance-bracket-list';
import {InsuranceBracketForm} from './insurance-brackets/pages/insurance-bracket-form/insurance-bracket-form';
import {PayrollList} from './payroll/pages/payroll-list/payroll-list';
import {PayrollForm} from './payroll/pages/payroll-form/payroll-form';
import {AdvanceList} from './advance-requests/pages/advance-list/advance-list';
import {AdvanceForm} from './advance-requests/pages/advance-form/advance-form';
import {AdvanceDetail} from './advance-requests/pages/advance-detail/advance-detail';
import {WriteOffList} from './write-off-requests/pages/write-off-list/write-off-list';
import {WriteOffRequestForm as WriteOffForm} from './write-off-requests/pages/write-off-form/write-off-form';
import {WriteOffRequestDetail as WriteOffDetail} from './write-off-requests/pages/write-off-detail/write-off-detail';
import {TravelWriteOffList} from './travel-write-off-requests/pages/travel-write-off-list/travel-write-off-list';
import {TravelWriteOffForm} from './travel-write-off-requests/pages/travel-write-off-form/travel-write-off-form';
import {TravelWriteOffDetail} from './travel-write-off-requests/pages/travel-write-off-detail/travel-write-off-detail';

export const ADMIN_ROUTES: Routes = [
  {path: '', redirectTo: 'users', pathMatch: 'full'},

  // 員工管理
  {path: 'users',                component: UserList,       canActivate: [permissionGuard], data: {title: '員工管理',   permission: 'users:read'}},
  {path: 'users/new',            component: UserForm,       canActivate: [permissionGuard], data: {title: '新增員工',   permission: 'users:write'}},
  {path: 'users/:id/edit',       component: UserForm,       canActivate: [permissionGuard], data: {title: '編輯員工',   permission: 'users:write'}},

  // 部門管理
  {path: 'departments',          component: DepartmentList, canActivate: [permissionGuard], data: {title: '部門管理',   permission: 'departments:read'}},
  {path: 'departments/new',      component: DepartmentForm, canActivate: [permissionGuard], data: {title: '新增部門',   permission: 'departments:write'}},
  {path: 'departments/:id/edit', component: DepartmentForm, canActivate: [permissionGuard], data: {title: '編輯部門',   permission: 'departments:write'}},

  // 職稱管理
  {path: 'job-titles',           component: JobTitleList,   canActivate: [permissionGuard], data: {title: '職稱管理',   permission: 'job-titles:read'}},
  {path: 'job-titles/new',       component: JobTitleForm,   canActivate: [permissionGuard], data: {title: '新增職稱',   permission: 'job-titles:write'}},
  {path: 'job-titles/:id/edit',  component: JobTitleForm,   canActivate: [permissionGuard], data: {title: '編輯職稱',   permission: 'job-titles:write'}},

  // 廠商管理
  {path: 'vendors',              component: VendorList,     canActivate: [permissionGuard], data: {title: '廠商管理',   permission: 'vendors:read'}},
  {path: 'vendors/new',          component: VendorForm,     canActivate: [permissionGuard], data: {title: '新增廠商',   permission: 'vendors:write'}},
  {path: 'vendors/:id/edit',     component: VendorForm,     canActivate: [permissionGuard], data: {title: '編輯廠商',   permission: 'vendors:write'}},

  // 簽核管理
  {path: 'approvals',            component: ApprovalList,   canActivate: [permissionGuard], data: {title: '簽核管理',   permission: 'approvals:read'}},
  {path: 'approvals/:id/flow',   component: ApprovalFlow,   canActivate: [permissionGuard], data: {title: '簽核流程',   permission: 'approvals:read'}},

  // 角色 / 權限
  {path: 'roles',                component: RoleList,       canActivate: [permissionGuard], data: {title: '角色管理',   permission: 'roles:read'}},
  {path: 'roles/new',            component: RoleForm,       canActivate: [permissionGuard], data: {title: '新增角色',   permission: 'roles:write'}},
  {path: 'roles/:id/edit',       component: RoleForm,       canActivate: [permissionGuard], data: {title: '編輯角色',   permission: 'roles:write'}},
  {path: 'permissions',          component: PermissionList, canActivate: [permissionGuard], data: {title: '權限管理',   permission: 'superadmin'}},
  {path: 'permissions/new',      component: PermissionForm, canActivate: [permissionGuard], data: {title: '新增權限',   permission: 'superadmin'}},
  {path: 'permissions/:id/edit', component: PermissionForm, canActivate: [permissionGuard], data: {title: '編輯權限',   permission: 'superadmin'}},

  // 專案管理
  {path: 'projects',             component: ProjectList,    canActivate: [permissionGuard], data: {title: '專案管理',       permission: 'projects:read'}},
  {path: 'projects/new',         component: ProjectForm,    canActivate: [permissionGuard], data: {title: '新增專案',       permission: 'projects:write'}},
  {path: 'projects/:id/edit',    component: ProjectForm,    canActivate: [permissionGuard], data: {title: '檢視專案',       permission: 'projects:read'}},

  // 預審申請
  {path: 'pre-review-requests',          canActivate: [permissionGuard], data: {title: '預審申請',       permission: 'pre-review-requests:read'  }, loadComponent: () => import('./pre-review-requests/pages/pre-review-list/pre-review-list').then(m => m.PreReviewList)},
  {path: 'pre-review-requests/new',      canActivate: [permissionGuard], data: {title: '新增預審申請',   permission: 'pre-review-requests:write' }, loadComponent: () => import('./pre-review-requests/pages/pre-review-form/pre-review-form').then(m => m.PreReviewForm)},
  {path: 'pre-review-requests/:id/edit', canActivate: [permissionGuard], data: {title: '編輯預審申請',   permission: 'pre-review-requests:read'  }, loadComponent: () => import('./pre-review-requests/pages/pre-review-form/pre-review-form').then(m => m.PreReviewForm)},
  {path: 'pre-review-requests/:id',      canActivate: [permissionGuard], data: {title: '預審申請詳情',   permission: 'pre-review-requests:read'  }, loadComponent: () => import('./pre-review-requests/pages/pre-review-detail/pre-review-detail').then(m => m.PreReviewDetail)},

  // 請款申請
  {path: 'payment-requests',             component: PaymentList, canActivate: [permissionGuard], data: {title: '請款申請',       permission: 'payment-requests:read'}},
  {path: 'payment-requests/new',         component: PaymentForm, canActivate: [permissionGuard], data: {title: '新增請款申請',   permission: 'payment-requests:write'}},
  {path: 'payment-requests/:id/edit',    component: PaymentForm, canActivate: [permissionGuard], data: {title: '編輯請款申請',   permission: 'payment-requests:read'}},
  {path: 'payment-requests/:id',         canActivate: [permissionGuard], data: {title: '請款申請詳情', permission: 'payment-requests:read'}, loadComponent: () => import('./payment-requests/pages/payment-detail/payment-detail').then(m => m.PaymentDetail)},

  // 預支申請
  {path: 'advance-requests',             component: AdvanceList,   canActivate: [permissionGuard], data: {title: '預支申請',       permission: 'advance-requests:read'}},
  {path: 'advance-requests/new',         component: AdvanceForm,   canActivate: [permissionGuard], data: {title: '新增預支申請',   permission: 'advance-requests:write'}},
  {path: 'advance-requests/:id/edit',    component: AdvanceForm,   canActivate: [permissionGuard], data: {title: '編輯預支申請',   permission: 'advance-requests:write'}},
  // 追加預支批次（須排在 :id 之前，Angular 路由先到先配）
  {path: 'advance-requests/:id/supplements/new',          component: AdvanceForm, canActivate: [permissionGuard], data: {title: '新增追加預支', permission: 'advance-requests:write', mode: 'supplement'}},
  {path: 'advance-requests/:id/supplements/:round/edit',  component: AdvanceForm, canActivate: [permissionGuard], data: {title: '編輯追加預支', permission: 'advance-requests:write', mode: 'supplement'}},
  {path: 'advance-requests/:id',         component: AdvanceDetail, canActivate: [permissionGuard], data: {title: '預支申請詳情',   permission: 'advance-requests:read'}},

  // 沖銷申請
  {path: 'write-off-requests',           component: WriteOffList,   canActivate: [permissionGuard], data: {title: '沖銷申請',       permission: 'write-off-requests:read'}},
  {path: 'write-off-requests/new',       component: WriteOffForm,   canActivate: [permissionGuard], data: {title: '新增沖銷申請',   permission: 'write-off-requests:write'}},
  {path: 'write-off-requests/:id/edit',  component: WriteOffForm,   canActivate: [permissionGuard], data: {title: '編輯沖銷申請',   permission: 'write-off-requests:write'}},
  {path: 'write-off-requests/:id',       component: WriteOffDetail, canActivate: [permissionGuard], data: {title: '沖銷申請詳情',   permission: 'write-off-requests:read'}},

  // 出差預支沖銷申請
  {path: 'travel-write-off-requests',           component: TravelWriteOffList,   canActivate: [permissionGuard], data: {title: '出差預支沖銷申請',       permission: 'travel-write-off-requests:read'}},
  {path: 'travel-write-off-requests/new',       component: TravelWriteOffForm,   canActivate: [permissionGuard], data: {title: '新增出差預支沖銷申請',   permission: 'travel-write-off-requests:write'}},
  {path: 'travel-write-off-requests/:id/edit',  component: TravelWriteOffForm,   canActivate: [permissionGuard], data: {title: '編輯出差預支沖銷申請',   permission: 'travel-write-off-requests:write'}},
  {path: 'travel-write-off-requests/:id',       component: TravelWriteOffDetail, canActivate: [permissionGuard], data: {title: '出差預支沖銷申請詳情',   permission: 'travel-write-off-requests:read'}},

  // 請假申請
  {path: 'leave-requests',             component: LeaveRequestList, canActivate: [permissionGuard], data: {title: '請假申請',       permission: 'leave-requests:read'}},
  {path: 'leave-requests/new',         component: LeaveRequestForm, canActivate: [permissionGuard], data: {title: '新增請假申請',   permission: 'leave-requests:write'}},
  {path: 'leave-requests/:id/edit',    component: LeaveRequestForm, canActivate: [permissionGuard], data: {title: '編輯請假申請',   permission: 'leave-requests:read'}},

  // 出差預支申請
  {path: 'travel-requests',             component: TravelRequestList, canActivate: [permissionGuard], data: {title: '出差預支申請',       permission: 'travel-requests:read'}},
  {path: 'travel-requests/new',         component: TravelRequestForm, canActivate: [permissionGuard], data: {title: '新增出差預支申請',   permission: 'travel-requests:write'}},
  {path: 'travel-requests/:id/edit',    component: TravelRequestForm, canActivate: [permissionGuard], data: {title: '編輯出差預支申請',   permission: 'travel-requests:read'}},
  {path: 'travel-requests/:id',         component: TravelDetail,      canActivate: [permissionGuard], data: {title: '出差預支申請詳情',   permission: 'travel-requests:read'}},

  // 出差請款申請
  {path: 'travel-payment-requests',          canActivate: [permissionGuard], data: {title: '出差請款申請',     permission: 'travel-payment-requests:read'  }, loadComponent: () => import('./travel-payment-requests/pages/travel-payment-list/travel-payment-list').then(m => m.TravelPaymentList)},
  {path: 'travel-payment-requests/new',      canActivate: [permissionGuard], data: {title: '新增出差請款申請', permission: 'travel-payment-requests:write' }, loadComponent: () => import('./travel-payment-requests/pages/travel-payment-form/travel-payment-form').then(m => m.TravelPaymentForm)},
  {path: 'travel-payment-requests/:id/edit', canActivate: [permissionGuard], data: {title: '編輯出差請款申請', permission: 'travel-payment-requests:read'  }, loadComponent: () => import('./travel-payment-requests/pages/travel-payment-form/travel-payment-form').then(m => m.TravelPaymentForm)},
  {path: 'travel-payment-requests/:id',      canActivate: [permissionGuard], data: {title: '出差請款申請詳情', permission: 'travel-payment-requests:read'  }, loadComponent: () => import('./travel-payment-requests/pages/travel-payment-detail/travel-payment-detail').then(m => m.TravelPaymentDetail)},

  // 假日執行活動申請
  {path: 'holiday-travel-requests',          canActivate: [permissionGuard], data: {title: '假日執行活動申請',       permission: 'holiday-travel-requests:read'}, loadComponent: () => import('./holiday-travel-requests/pages/holiday-travel-request-list/holiday-travel-request-list').then(m => m.HolidayTravelRequestList)},
  {path: 'holiday-travel-requests/new',      canActivate: [permissionGuard], data: {title: '新增假日執行活動申請',   permission: 'holiday-travel-requests:write'}, loadComponent: () => import('./holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form').then(m => m.HolidayTravelRequestForm)},
  {path: 'holiday-travel-requests/:id/edit', canActivate: [permissionGuard], data: {title: '編輯假日執行活動申請',   permission: 'holiday-travel-requests:read'}, loadComponent: () => import('./holiday-travel-requests/pages/holiday-travel-request-form/holiday-travel-request-form').then(m => m.HolidayTravelRequestForm)},
  {path: 'holiday-travel-requests/:id',      canActivate: [permissionGuard], data: {title: '假日執行活動申請詳情',   permission: 'holiday-travel-requests:read'}, loadComponent: () => import('./holiday-travel-requests/pages/holiday-travel-detail/holiday-travel-detail').then(m => m.HolidayTravelDetail)},

  // 行事曆管理
  {path: 'calendar-days',                    canActivate: [permissionGuard], data: {title: '行事曆管理',         permission: 'calendar-days:read'}, loadComponent: () => import('./calendar-days/pages/calendar-day-list/calendar-day-list').then(m => m.CalendarDayList)},

  // 加班申請
  {path: 'overtime-requests',             component: OvertimeRequestList, canActivate: [permissionGuard], data: {title: '加班申請',       permission: 'overtime-requests:read'}},
  {path: 'overtime-requests/new',         component: OvertimeRequestForm, canActivate: [permissionGuard], data: {title: '新增加班申請',   permission: 'overtime-requests:write'}},
  {path: 'overtime-requests/:id/edit',    component: OvertimeRequestForm, canActivate: [permissionGuard], data: {title: '編輯加班申請',   permission: 'overtime-requests:read'}},

  // 簽核作業
  {path: 'approval-tasks',                                   component: ApprovalTaskList,   canActivate: [permissionGuard], data: {title: '簽核作業',   permission: 'approval-tasks:read'}},
  {path: 'approval-tasks/:applicationType/:id/review',       component: ApprovalTaskReview, canActivate: [permissionGuard], data: {title: '審核',       permission: 'approval-tasks:read'}},

  // 統計報表
  {path: 'reports/attendance',   component: AttendanceReport, canActivate: [permissionGuard], data: {title: '出缺勤紀錄', permission: 'reports-attendance:read'}},
  {path: 'reports/overtime',    component: OvertimeReport,   canActivate: [permissionGuard], data: {title: '加班紀錄',   permission: 'reports-overtime:read'}},
  {path: 'reports/payment',    component: PaymentReport,    canActivate: [permissionGuard], data: {title: '款項統計',   permission: 'reports-payment:read'}},
  {path: 'reports/project-water-level', component: ProjectWaterLevel, canActivate: [permissionGuard], data: {title: '專案水位表', permission: 'reports-project-water-level:read'}},

  // 人事薪資
  {path: 'payroll',                       component: PayrollList,          canActivate: [permissionGuard], data: {title: '人事薪資',       permission: 'payroll:read'}},
  {path: 'payroll/:id/edit',              component: PayrollForm,          canActivate: [permissionGuard], data: {title: '薪資調整',       permission: 'payroll:write'}},

  // 勞健保級距
  {path: 'insurance-brackets',             component: InsuranceBracketList, canActivate: [permissionGuard], data: {title: '勞健保級距維護', permission: 'insurance-brackets:read'}},
  {path: 'insurance-brackets/new',         component: InsuranceBracketForm, canActivate: [permissionGuard], data: {title: '新增勞健保級距', permission: 'insurance-brackets:write'}},
  {path: 'insurance-brackets/:id/edit',    component: InsuranceBracketForm, canActivate: [permissionGuard], data: {title: '編輯勞健保級距', permission: 'insurance-brackets:write'}},

  // 打卡提醒紀錄（Superadmin only）
  {path: 'attendance-reminder-logs',                  canActivate: [permissionGuard], data: {title: '打卡提醒紀錄',     permission: 'superadmin'}, loadComponent: () => import('./attendance-reminder-logs/pages/attendance-reminder-log-list/attendance-reminder-log-list').then(m => m.AttendanceReminderLogList)},
  {path: 'attendance-reminder-logs/batches/:batchId', canActivate: [permissionGuard], data: {title: '打卡提醒批次詳情', permission: 'superadmin'}, loadComponent: () => import('./attendance-reminder-logs/pages/attendance-reminder-log-detail/attendance-reminder-log-detail').then(m => m.AttendanceReminderLogDetail)},

  // 撥款提醒紀錄（Superadmin only）
  {path: 'payment-reminder-logs', canActivate: [permissionGuard], data: {title: '撥款提醒紀錄', permission: 'superadmin'}, loadComponent: () => import('./payment-reminder-logs/pages/payment-reminder-log-list/payment-reminder-log-list').then(m => m.PaymentReminderLogList)},

  // 系統設定
  {path: 'settings',             component: Settings,       canActivate: [permissionGuard], data: {title: '系統設定',   permission: 'settings:read'}},
];
