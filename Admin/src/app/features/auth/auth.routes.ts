import {Routes} from '@angular/router';
import {Login} from './pages/login/login';
import {Register} from './pages/register/register';
import {LockScreen} from './pages/lock-screen/lock-screen';
import {TwoFactor} from './pages/two-factor/two-factor';
import {ForgotPassword} from './pages/forgot-password/forgot-password';



export const AUTH_ROUTES: Routes = [
  {
    path: 'auth/login',
    component: Login,
    data: {title: "Login"},
  },
  {
    path: 'auth/register',
    component: Register,
    data: {title: "Register"},
  },
  {
    path: 'auth/lockscreen',
    component: LockScreen,
    data: {title: "Lockscreen"},
  },
  {
    path: 'auth/two-factor',
    component: TwoFactor,
    data: {title: "Two Factor"},
  },
  {
    path: 'auth/forgot-password',
    component: ForgotPassword,
    data: {title: "Forgot Password"},
  },

];
