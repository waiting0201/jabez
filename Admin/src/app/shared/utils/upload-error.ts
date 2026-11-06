import {HttpErrorResponse} from '@angular/common/http';

/**
 * 含檔案上傳（multipart）的申請表單：請求大小上限與錯誤訊息的單一真相。
 *
 * 起因（2026-11-06 正式站）：同仁一張預支沖銷 90 筆明細、各附一張發票圖，整包超過後端上限，
 * 後端在讀取本文時就切斷連線 → 前端拿不到任何 `message`，只顯示「儲存失敗，請稍後再試」，
 * 同仁連按 7 次都一樣、完全不知道原因。
 *
 * 兩道防線：
 * 1. 送出前以 {@link uploadTooLargeMessage} 預估 FormData 大小，超過就直接擋下並說明（不浪費一次上傳）；
 * 2. 真的送出後失敗，以 {@link saveErrorMessage} 把 413 / 連線中斷（status 0）翻成看得懂的訊息。
 *
 * 上限數字與後端 `Api/Common/RequestBodyLimits.cs` 必須一起改（後端再往上調還要改 Functions host 的
 * `FUNCTIONS_REQUEST_BODY_SIZE_LIMIT`）。預估時預留 multipart 邊界與欄位文字的空間，故比上限略小。
 */
export const MAX_UPLOAD_BYTES = 100 * 1024 * 1024;

/** 預估門檻：留 2MB 給 multipart 邊界、items JSON 等文字欄位 */
const PRECHECK_BYTES = MAX_UPLOAD_BYTES - 2 * 1024 * 1024;

const TOO_LARGE_MESSAGE =
  '上傳檔案總量超過 100MB，請分成兩張以上的申請單送出，或改以較小的檔案（例如拍照而非掃描 PDF）上傳。';

/** FormData 內所有檔案的位元組總和（文字欄位忽略不計） */
function formDataFileBytes(fd: FormData): number {
  let total = 0;
  fd.forEach(v => {
    if (v instanceof Blob) total += v.size;
  });
  return total;
}

/**
 * 送出前檢查：檔案總量超過上限回傳錯誤訊息（含目前大小），未超過回 null。
 */
export function uploadTooLargeMessage(fd: FormData): string | null {
  const bytes = formDataFileBytes(fd);
  if (bytes <= PRECHECK_BYTES) return null;
  const mb = (bytes / 1024 / 1024).toFixed(1);
  return `${TOO_LARGE_MESSAGE}（目前約 ${mb}MB）`;
}

/**
 * 儲存 / 送出失敗時的顯示訊息：後端有 `message` 就用後端的；
 * 413 → 檔案過大；status 0（連線被切斷 / 網路中斷）→ 提示可能是檔案過大或網路不穩；其餘用 fallback。
 */
export function saveErrorMessage(err: HttpErrorResponse, fallback: string): string {
  const serverMsg = err.error?.message;
  if (serverMsg) return serverMsg;
  if (err.status === 413) return TOO_LARGE_MESSAGE;
  if (err.status === 0) {
    return '連線中斷，未能完成儲存。可能是上傳檔案過多過大或網路不穩，'
      + '請確認網路後再試；若仍失敗，請將明細分成兩張以上的申請單送出。';
  }
  return fallback;
}
