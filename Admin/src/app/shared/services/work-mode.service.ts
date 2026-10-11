import {Injectable, inject, signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {environment} from '@/environments/environment';

export interface WorkMode {
  /** 四週彈性工時切換日；null ＝ 尚未切換，全系統維持現制 */
  flexibleWorkStartDate: string | null;
  /** 以**今天**判斷是否已生效。只供 UI 開關使用，不可用於資料解讀（見下方註解） */
  isFlexibleActive: boolean;
}

/**
 * 「四週彈性工時切換了沒」的前端取用管道（全站唯一入口）。
 *
 * 走輕量端點 `GET /work-mode`（任何登入者，免 `settings:read`）——
 * 一般同仁為了知道制度而拿到整份系統設定，等於把後台權限強加給員工。
 *
 * **request-scoped 快取**：切換日在一次瀏覽期間不會變，載入一次即可；
 * 同時避免多個元件各自打一次。失敗時退回「尚未切換」（安全側：寧可讓入口留著，
 * 也不要因為一次網路失敗就把功能整個藏起來、使用者完全無從自救）。
 *
 * ⚠ `isFlexibleActive` 是**以今天**判斷，僅供 UI 開關（例如要不要顯示某個新增按鈕）。
 *   任何與**資料**有關的判定（薪資、加班費率、請假時段）一律由後端以
 *   **該筆資料自己的日期**比對切換日，前端不得自行重算，否則切換後歷史資料會被新制重新解讀。
 */
@Injectable({providedIn: 'root'})
export class WorkModeService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/work-mode`;

  /** 已載入的狀態；未載入前為 null，元件以 `@if` 或預設值處理 */
  readonly mode = signal<WorkMode | null>(null);

  private pending: Promise<WorkMode> | null = null;

  /** 載入一次並快取。重複呼叫共用同一個 in-flight promise。 */
  async load(): Promise<WorkMode> {
    const cached = this.mode();
    if (cached) return cached;
    if (this.pending) return this.pending;

    this.pending = firstValueFrom(this.http.get<WorkMode>(this.base))
      .then(m => {
        this.mode.set(m);
        return m;
      })
      .catch(() => {
        // 退回「尚未切換」：讓既有入口維持可見，而不是因一次失敗就整個消失
        const fallback: WorkMode = {flexibleWorkStartDate: null, isFlexibleActive: false};
        this.mode.set(fallback);
        return fallback;
      })
      .finally(() => {
        this.pending = null;
      });

    return this.pending;
  }
}
