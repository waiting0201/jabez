export interface Role {
  id: string;
  name: string;
  description?: string;
  permissionCodes: string[];
  createdAt: Date;
}
