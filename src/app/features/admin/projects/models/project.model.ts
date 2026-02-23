export interface Project {
  id: number;
  code: string;
  departmentId?: number;
  departmentName?: string;
  budgetAmount?: number;
  actualAmount?: number;
  businessAmount?: number;
  googleDriveUrl?: string;
  createdAt: Date;
}
