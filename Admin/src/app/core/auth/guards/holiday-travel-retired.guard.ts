import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {WorkModeService} from '@shared/services/work-mode.service';

/**
 * 〈假日執行活動申請〉新增 / 編輯路由的退場守衛（四週彈性工時 §8）。
 *
 * 該申請別**只關寫入口、保留唯讀** —— 列表與詳情不掛此守衛。
 * 真正的閘門是後端 `TravelRequestHandler.GuardHolidayTravelRetiredAsync`；
 * 本守衛只是避免使用者直接打網址進到表單、填完才在送出時被擋。
 *
 * ⚠ 導向**列表**而非 403：這不是權限不足，是功能退場，導到 403 會讓人以為自己被降權。
 * 切換日未設定時 `isFlexibleActive` 為 false，守衛直接放行，故本階段部署不改變任何現行行為。
 */
export const holidayTravelRetiredGuard: CanActivateFn = async () => {
  const workMode = inject(WorkModeService);
  const router   = inject(Router);
  const toastr   = inject(ToastrService);

  const mode = await workMode.load();
  if (!mode.isFlexibleActive) return true;

  toastr.info('假日執行活動申請已退場，假日出勤請改提〈加班申請〉。', '功能已停用');
  return router.createUrlTree(['/admin/holiday-travel-requests']);
};
