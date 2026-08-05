import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {AuthService} from '../services/auth.service';

export const noAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  // 導到 '/' 而非 '/dashboard'：實際落點由 app.routes.ts 的首頁決策點決定（見 resolveLandingUrl）
  return auth.isLoggedIn() ? router.createUrlTree(['/']) : true;
};
