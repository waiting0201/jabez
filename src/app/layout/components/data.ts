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

  { label: '系統管理', isTitle: true },
  {
    icon: '/assets/icons/sprite.svg#users',
    label: '組織管理',
    isCollapsed: true,
    requiredPermission: 'admin:access',
    children: [
      { label: '員工管理', url: '/admin/users', requiredPermission: 'users:read' },
      { label: '部門管理', url: '/admin/departments', requiredPermission: 'departments:read' },
      { label: '職稱管理', url: '/admin/job-titles', requiredPermission: 'job-titles:read' },
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
    icon: '/assets/icons/sprite.svg#shield',
    label: '系統權限',
    isCollapsed: true,
    requiredPermission: 'superadmin',
    children: [
      { label: '角色管理', url: '/admin/roles', requiredPermission: 'superadmin' },
      { label: '權限管理', url: '/admin/permissions', requiredPermission: 'superadmin' },
    ],
  },
  {
    icon: '/assets/icons/sprite.svg#settings',
    label: '系統設定',
    url: '/admin/settings',
    requiredPermission: 'admin:access',
  },
];
