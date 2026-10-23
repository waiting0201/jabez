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
  /** 工作地縣市代碼（颱風假用；null＝沿用上層部門） */
  county?: string | null;
  employeeCount: number;
  employeeNames?: string;
  createdAt: Date;
}
