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
import {OvertimeRequestList} from './overtime-requests/pages/overtime-request-list/overtime-request-list';
import {OvertimeRequestForm} from './overtime-requests/pages/overtime-request-form/overtime-request-form';

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

  // 簽核管理
  {path: 'approvals',            component: ApprovalList,   canActivate: [permissionGuard], data: {title: '簽核管理',   permission: 'approvals:read'}},
  {path: 'approvals/:id/flow',   component: ApprovalFlow,   canActivate: [permissionGuard], data: {title: '簽核流程',   permission: 'approvals:read'}},

  // 角色 / 權限（僅超管帳號可存取）
  {path: 'roles',                component: RoleList,       canActivate: [permissionGuard], data: {title: '角色管理',   permission: 'superadmin'}},
  {path: 'roles/new',            component: RoleForm,       canActivate: [permissionGuard], data: {title: '新增角色',   permission: 'superadmin'}},
  {path: 'roles/:id/edit',       component: RoleForm,       canActivate: [permissionGuard], data: {title: '編輯角色',   permission: 'superadmin'}},
  {path: 'permissions',          component: PermissionList, canActivate: [permissionGuard], data: {title: '權限管理',   permission: 'superadmin'}},
  {path: 'permissions/new',      component: PermissionForm, canActivate: [permissionGuard], data: {title: '新增權限',   permission: 'superadmin'}},
  {path: 'permissions/:id/edit', component: PermissionForm, canActivate: [permissionGuard], data: {title: '編輯權限',   permission: 'superadmin'}},

  // 專案管理
  {path: 'projects',             component: ProjectList,    canActivate: [permissionGuard], data: {title: '專案管理',       permission: 'projects:read'}},
  {path: 'projects/new',         component: ProjectForm,    canActivate: [permissionGuard], data: {title: '新增專案',       permission: 'projects:write'}},
  {path: 'projects/:id/edit',    component: ProjectForm,    canActivate: [permissionGuard], data: {title: '編輯專案',       permission: 'projects:write'}},

  // 請款申請
  {path: 'payment-requests',             component: PaymentList, canActivate: [permissionGuard], data: {title: '請款申請',       permission: 'payment-requests:read'}},
  {path: 'payment-requests/new',         component: PaymentForm, canActivate: [permissionGuard], data: {title: '新增請款申請',   permission: 'payment-requests:write'}},
  {path: 'payment-requests/:id/edit',    component: PaymentForm, canActivate: [permissionGuard], data: {title: '編輯請款申請',   permission: 'payment-requests:read'}},

  // 請假申請
  {path: 'leave-requests',             component: LeaveRequestList, canActivate: [permissionGuard], data: {title: '請假申請',       permission: 'leave-requests:read'}},
  {path: 'leave-requests/new',         component: LeaveRequestForm, canActivate: [permissionGuard], data: {title: '新增請假申請',   permission: 'leave-requests:write'}},
  {path: 'leave-requests/:id/edit',    component: LeaveRequestForm, canActivate: [permissionGuard], data: {title: '編輯請假申請',   permission: 'leave-requests:read'}},

  // 出差申請
  {path: 'travel-requests',             component: TravelRequestList, canActivate: [permissionGuard], data: {title: '出差申請',       permission: 'travel-requests:read'}},
  {path: 'travel-requests/new',         component: TravelRequestForm, canActivate: [permissionGuard], data: {title: '新增出差申請',   permission: 'travel-requests:write'}},
  {path: 'travel-requests/:id/edit',    component: TravelRequestForm, canActivate: [permissionGuard], data: {title: '編輯出差申請',   permission: 'travel-requests:read'}},

  // 加班申請
  {path: 'overtime-requests',             component: OvertimeRequestList, canActivate: [permissionGuard], data: {title: '加班申請',       permission: 'overtime-requests:read'}},
  {path: 'overtime-requests/new',         component: OvertimeRequestForm, canActivate: [permissionGuard], data: {title: '新增加班申請',   permission: 'overtime-requests:write'}},
  {path: 'overtime-requests/:id/edit',    component: OvertimeRequestForm, canActivate: [permissionGuard], data: {title: '編輯加班申請',   permission: 'overtime-requests:read'}},

  // 簽核作業
  {path: 'approval-tasks',                                   component: ApprovalTaskList,   canActivate: [permissionGuard], data: {title: '簽核作業',   permission: 'approval-tasks:read'}},
  {path: 'approval-tasks/:applicationType/:id/review',       component: ApprovalTaskReview, canActivate: [permissionGuard], data: {title: '審核',       permission: 'approval-tasks:write'}},

  // 系統設定
  {path: 'settings',             component: Settings,       canActivate: [permissionGuard], data: {title: '系統設定',   permission: 'admin:access'}},
];
