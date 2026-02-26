import {Routes} from '@angular/router';
import {MainLayout} from '@layout/main-layout/main-layout';
import {AuthLayout} from '@layout/auth-layout/auth-layout';
import {authGuard} from '@core/auth/guards/auth.guard';
import {noAuthGuard} from '@core/auth/guards/no-auth.guard';
import {Login} from '@features/auth/pages/login/login';
import {Register} from '@features/auth/pages/register/register';
import {ForgotPassword} from '@features/auth/pages/forgot-password/forgot-password';
import {LockScreen} from '@features/auth/pages/lock-screen/lock-screen';
import {TwoFactor} from '@features/auth/pages/two-factor/two-factor';
import {Error403} from '@features/error/pages/error-403/error-403';
import {Error404} from '@features/error/pages/error-404/error-404';
import {Error404Alt} from '@features/error/pages/error-404-alt/error-404-alt';
import {Error500} from '@features/error/pages/error-500/error-500';

export const routes: Routes = [
  {path: '', redirectTo: 'dashboard', pathMatch: 'full'},

  // 主版面（需登入）
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES),
      },
      {
        path: 'admin',
        loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES),
      },
      {path: 'error/404', component: Error404, data: {title: 'Error 404'}},
      {path: 'error/403', component: Error403, data: {title: 'Error 403'}},
    ],
  },

  // Auth 版面（已登入者透過 noAuthGuard 導回 dashboard）
  {
    path: '',
    component: AuthLayout,
    children: [
      {path: 'auth/login',           component: Login,          canActivate: [noAuthGuard], data: {title: 'Login'}},
      {path: 'auth/register',        component: Register,       canActivate: [noAuthGuard], data: {title: 'Register'}},
      {path: 'auth/forgot-password', component: ForgotPassword, data: {title: 'Forgot Password'}},
      {path: 'auth/lockscreen',      component: LockScreen,     data: {title: 'Lock Screen'}},
      {path: 'auth/two-factor',      component: TwoFactor,      data: {title: 'Two Factor'}},
    ],
  },

  {path: 'error/404-2', component: Error404Alt, data: {title: 'Error 404'}},
  {path: 'error/500',   component: Error500,    data: {title: 'Error 500'}},
  {path: '**', redirectTo: 'error/404'},
];
