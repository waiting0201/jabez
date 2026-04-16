import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '@/environments/environment';

export interface LineBindUrlResponse {
  url: string;
  state: string;
}

export interface LineBindingStatus {
  isBound: boolean;
  lineLinkedAt: string | null;
  isBotFriend: boolean;
}

@Injectable({ providedIn: 'root' })
export class LineService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /** 共享綁定狀態 signal — ProfileDropdown 與 LineBindCallback 共用 */
  isBound = signal(false);
  /** OA 好友狀態（綁定完成但未加 OA 為好友時推播會失敗） */
  isBotFriend = signal(false);

  /** 取得 LINE OAuth 綁定 URL */
  getBindUrl(): Observable<LineBindUrlResponse> {
    return this.http.get<LineBindUrlResponse>(`${this.apiUrl}/line/bind-url`);
  }

  /** 用 OAuth code 完成綁定 */
  bind(code: string, redirectUri: string): Observable<LineBindingStatus> {
    return this.http.post<LineBindingStatus>(`${this.apiUrl}/line/bind`, { code, redirectUri })
      .pipe(tap(status => this.applyStatus(status)));
  }

  /** 解除 LINE 綁定 */
  unbind(): Observable<LineBindingStatus> {
    return this.http.post<LineBindingStatus>(`${this.apiUrl}/line/unbind`, {})
      .pipe(tap(status => this.applyStatus(status)));
  }

  /** 查詢 LINE 綁定狀態（同步更新 signal） */
  refreshStatus(): void {
    this.http.get<LineBindingStatus>(`${this.apiUrl}/line/binding-status`).subscribe({
      next: (status) => this.applyStatus(status),
      error: () => {},
    });
  }

  private applyStatus(status: LineBindingStatus): void {
    this.isBound.set(status.isBound);
    this.isBotFriend.set(status.isBotFriend);
  }
}
