import {HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse} from '@angular/common/http';
import {map} from 'rxjs/operators';

interface ApiResponse<T> {
  success: boolean;
  data:    T;
  message: string;
  errors:  string[];
}

/**
 * 解包後端 ApiResponse<T> 包裝層。
 * 後端所有回應格式：{ success, data, message, errors, timestamp }
 * 此 Interceptor 將 HttpResponse.body 替換為 body.data，
 * 讓各 Service 可直接 this.http.get<User[]>(...) 而不需手動 .pipe(map(r => r.data))。
 */
export const apiResponseInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  return next(req).pipe(
    map(event => {
      if (
        event instanceof HttpResponse &&
        event.body !== null &&
        typeof event.body === 'object' &&
        'success' in event.body &&
        'data' in event.body
      ) {
        const api = event.body as ApiResponse<unknown>;
        return event.clone({ body: api.data });
      }
      return event;
    }),
  );
};
