import {inject} from '@angular/core';
import {CanActivateChildFn, Router} from '@angular/router';
import {AuthService} from '../services/auth.service';

/**
 * 強制改密碼的全域守衛（掛在 MainLayout 的 canActivateChild）。
 *
 * token 帶 `pwd_change_required` claim 時，主版面內只准進改密碼頁，其餘一律導向
 * `/account/change-password?forced=1`。真正的閘門在後端 `AppRouter`（其餘端點一律 403），
 * 本守衛只是讓使用者看到的是「請先改密碼」而非一堆載入失敗。
 * 登入頁的導頁只在登入當下有效，重新整理或手打網址時靠的就是這支。
 */
export const passwordChangeGuard: CanActivateChildFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.mustChangePassword()) return true;
  const path = state.url.split('?')[0];
  if (path === '/account/change-password') return true;
  return router.createUrlTree(['/account/change-password'], {queryParams: {forced: '1'}});
};
