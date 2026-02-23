export interface Department {
  id: number;
  name: string;
  code?: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  sortOrder: number;
  employeeCount: number;
  createdAt: Date;
}
