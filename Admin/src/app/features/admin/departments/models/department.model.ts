export interface Department {
  id: number;
  name: string;
  code?: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  sortOrder: number;
  canViewSiblings: boolean;
  canSeeAll: boolean;
  canViewDescendants: boolean;
  canViewParent: boolean;
  employeeCount: number;
  employeeNames?: string;
  createdAt: Date;
}
