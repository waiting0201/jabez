import {AuthService} from '@core/auth/services/auth.service';

/**
 * 登入後的落地頁決策（單一真相）。
 *
 * 2026-08 打卡納入權限管理後，`/dashboard` 不再是人人都進得去的頁面，
 * 因此所有「回到主頁」一律指向 `/`，由這裡集中決定實際落點：
 *   有 attendances:read → /dashboard（打卡頁，絕大多數員工）
 *   否則               → /account/my-profile（個人資訊，任何登入者皆可進，只需 authGuard）
 *
 * 不要再有任何地方硬寫 `/dashboard` 當首頁 —— 那會造成
 * 「403 → 點回到主頁 → 又 403」的無窮迴圈。
 */
export function resolveLandingUrl(auth: AuthService): string {
  return auth.hasPermission('attendances:read') ? '/dashboard' : '/account/my-profile';
}
