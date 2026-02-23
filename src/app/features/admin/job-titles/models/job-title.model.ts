export interface JobTitle {
  id: number;
  name: string;
  level: number;
  description?: string;
  employeeCount: number;
  createdAt: Date;
}
