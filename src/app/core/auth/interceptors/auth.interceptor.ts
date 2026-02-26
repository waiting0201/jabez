import {HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest} from '@angular/common/http';
import {inject} from '@angular/core';
import {BehaviorSubject, catchError, filter, switchMap, take, throwError} from 'rxjs';
import {Router} from '@angular/router';
import {AuthService} from '../services/auth.service';

/** 不需要 token 的路徑（登入、刷新本身） */
const SKIP_PATHS = ['/auth/login', '/auth/refresh'];

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // 附加 Bearer token
  const token = auth.token;
  const authReq = token
    ? req.clone({setHeaders: {Authorization: `Bearer ${token}`}})
    : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // 只處理 401，且排除 auth 路徑避免無限迴圈
      if (err.status !== 401 || SKIP_PATHS.some(p => req.url.includes(p))) {
        return throwError(() => err);
      }

      if (!isRefreshing) {
        isRefreshing = true;
        refreshTokenSubject.next(null);

        if (!auth.refreshTokenValue) {
          isRefreshing = false;
          auth.logout();
          router.navigateByUrl('/auth/login');
          return throwError(() => err);
        }

        return auth.refreshAccessToken().pipe(
          switchMap(res => {
            isRefreshing = false;
            refreshTokenSubject.next(res.access_token);
            // 用新 token 重送原始請求
            return next(req.clone({
              setHeaders: {Authorization: `Bearer ${res.access_token}`},
            }));
          }),
          catchError(refreshErr => {
            isRefreshing = false;
            refreshTokenSubject.next(null);
            auth.logout();
            router.navigateByUrl('/auth/login');
            return throwError(() => refreshErr);
          }),
        );
      }

      // 另一個請求正在 refresh，等待完成後重試
      return refreshTokenSubject.pipe(
        filter(t => t !== null),
        take(1),
        switchMap(newToken => next(req.clone({
          setHeaders: {Authorization: `Bearer ${newToken}`},
        }))),
      );
    }),
  );
};
