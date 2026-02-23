export interface Permission {
  id: string;
  code: string;        // e.g. 'users:read', 'users:write', 'users:delete'
  name: string;
  module: string;      // e.g. 'Users', 'Roles', 'Permissions'
  description?: string;
}
