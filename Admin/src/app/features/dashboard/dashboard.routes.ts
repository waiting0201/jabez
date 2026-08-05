import {Routes} from '@angular/router';
import {permissionGuard} from '@core/auth/guards/permission.guard';
import {Dashboard} from './pages/dashboard/dashboard';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: Dashboard,
    canActivate: [permissionGuard],
    data: {title: 'Dashboard', permission: 'attendances:read'},
  },
];
