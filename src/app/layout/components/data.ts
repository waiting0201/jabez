import { MenuItemType } from '@core/layout/models/layout.model';

export const menuItems: MenuItemType[] = [
  { label: '主選單', isTitle: true },
  {
    icon: '/assets/icons/sprite.svg#clock',
    label: '打卡',
    url: '/dashboard',
  },

  { label: '業務管理', isTitle: true },
  {
    icon: '/assets/icons/sprite.svg#folder',
    label: '專案管理',
    url: '/admin/projects',
    requiredPermission: 'projects:read',
  },
  {
    icon: '/assets/icons/sprite.svg#dollar-sign',
    label: '請款申請',
    url: '/admin/payment-requests',
    requiredPermission: 'payment-requests:read',
  },
  {
    icon: '/assets/icons/sprite.svg#calendar',
    label: '請假申請',
    url: '/admin/leave-requests',
    requiredPermission: 'leave-requests:read',
  },
  {
    icon: '/assets/icons/sprite.svg#map-pin',
    label: '出差申請',
    url: '/admin/travel-requests',
    requiredPermission: 'travel-requests:read',
  },
  {
    icon: '/assets/icons/sprite.svg#zap',
    label: '加班申請',
    url: '/admin/overtime-requests',
    requiredPermission: 'overtime-requests:read',
  },
  {
    icon: '/assets/icons/sprite.svg#check-square',
    label: '簽核作業',
    url: '/admin/approval-tasks',
    requiredPermission: 'approval-tasks:read',
  },

  { label: '統計報表', isTitle: true },
  {
    icon: '/assets/icons/sprite.svg#bar-chart-2',
    label: '出缺勤紀錄',
    url: '/admin/reports/attendance',
    requiredPermission: 'reports-attendance:read',
  },
  {
    icon: '/assets/icons/sprite.svg#clock',
    label: '加班紀錄',
    url: '/admin/reports/overtime',
    requiredPermission: 'reports-overtime:read',
  },
  {
    icon: '/assets/icons/sprite.svg#credit-card',
    label: '請款統計',
    url: '/admin/reports/payment',
    requiredPermission: 'reports-payment:read',
  },
  {
    icon: '/assets/icons/sprite.svg#trending-up',
    label: '專案水位表',
    url: '/admin/reports/project-water-level',
    requiredPermission: 'reports-project-water-level:read',
  },
  { label: '系統管理', isTitle: true },
  {
    icon: '/assets/icons/sprite.svg#users',
    label: '組織管理',
    isCollapsed: true,
    children: [
      { label: '員工管理', url: '/admin/users', requiredPermission: 'users:read' },
      { label: '部門管理', url: '/admin/departments', requiredPermission: 'departments:read' },
      { label: '職稱管理', url: '/admin/job-titles', requiredPermission: 'job-titles:read' },
      { label: '人事薪資', url: '/admin/payroll', requiredPermission: 'payroll:read' },
    ],
  },
  {
    icon: '/assets/icons/sprite.svg#check-square',
    label: '簽核管理',
    isCollapsed: true,
    requiredPermission: 'approvals:read',
    children: [
      { label: '簽核項目', url: '/admin/approvals', requiredPermission: 'approvals:read' },
    ],
  },
  {
    icon: '/assets/icons/sprite.svg#file-text',
    label: '勞健保級距',
    url: '/admin/insurance-brackets',
    requiredPermission: 'insurance-brackets:read',
  },
  {
    icon: '/assets/icons/sprite.svg#shield',
    label: '系統權限',
    isCollapsed: true,
    requiredPermission: 'roles:read',
    children: [
      { label: '角色管理', url: '/admin/roles', requiredPermission: 'roles:read' },
      { label: '權限管理', url: '/admin/permissions', requiredPermission: 'permissions:read' },
    ],
  },
  {
    icon: '/assets/icons/sprite.svg#settings',
    label: '系統設定',
    url: '/admin/settings',
    requiredPermission: 'settings:read',
  },
];
