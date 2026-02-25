export type ProjectStatus = 'active' | 'closed';

export interface Project {
  id: number;
  code: string;
  status: ProjectStatus;
  departmentId?: number;
  departmentName?: string;
  budgetAmount?: number;
  actualAmount?: number;
  businessAmount?: number;
  googleDriveUrl?: string;
  createdAt: Date;
}

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  active: '進行中',
  closed: '已結案',
};

export const PROJECT_STATUS_CLASSES: Record<ProjectStatus, string> = {
  active: 'bg-success-subtle text-success',
  closed: 'bg-secondary-subtle text-secondary',
};
