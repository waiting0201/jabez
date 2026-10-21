import {ChangeDetectionStrategy, Component, ElementRef, OnDestroy, AfterViewInit, inject, signal, viewChild} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {NgbActiveModal, NgbModal} from '@ng-bootstrap/ng-bootstrap';

/** 一次最多拍幾張（避免一口氣塞爆明細表與 OCR 併發） */
const MAX_SHOTS = 20;

interface Shot {
  uid: number;
  file: File;
  url: string;   // 縮圖用 object URL，modal 關閉時 revoke
}

/** 瀏覽器是否支援鏡頭擷取（需 HTTPS 或 localhost）。不支援時呼叫端不顯示「用鏡頭拍攝」按鈕 */
export function isWebcamSupported(): boolean {
  return typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getUserMedia;
}

/**
 * 開啟鏡頭拍攝視窗，回傳拍好的 JPEG 檔（使用者取消時回空陣列）。
 * 拍出來的就是一般圖檔，呼叫端直接交給既有的選檔處理流程（HEIC 轉檔 / 壓縮 / OCR）即可。
 */
export async function openWebcamCapture(modal: NgbModal): Promise<File[]> {
  const ref = modal.open(WebcamCaptureModal, {size: 'lg', centered: true, backdrop: 'static'});
  try {
    return (await ref.result) as File[];
  } catch {
    return [];
  }
}

/**
 * 用電腦 / 手機鏡頭拍照的共用視窗（連拍：每張按「拍照」，全部拍完按「完成」才交回呼叫端）。
 *
 * ⚠ 鏡頭串流必須在關閉時 stop，否則鏡頭指示燈會一直亮著、其他程式也拿不到鏡頭 ——
 * close / dismiss / 換鏡頭 / destroy 四條路一律走 `stopStream()`。
 */
@Component({
  selector: 'app-webcam-capture-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="modal-header">
      <h5 class="modal-title flex items-center gap-2">
        <svg class="sa-icon text-primary" style="stroke: currentColor">
          <use href="/assets/icons/sprite.svg#camera"></use>
        </svg>
        用鏡頭拍攝
      </h5>
      <button type="button" class="btn-close" (click)="cancel()"></button>
    </div>

    <div class="modal-body">
      @if (error()) {
        <div class="alert alert-warning small mb-3">
          <div>{{ error() }}</div>
        </div>
      }

      @if (devices().length > 1) {
        <div class="mb-3">
          <label class="form-label small text-muted mb-1">鏡頭</label>
          <select class="form-select form-select-sm" [ngModel]="deviceId()" (ngModelChange)="switchDevice($event)">
            @for (d of devices(); track d.deviceId; let i = $index) {
              <option [value]="d.deviceId">{{ d.label || ('鏡頭 ' + (i + 1)) }}</option>
            }
          </select>
        </div>
      }

      <div class="relative rounded overflow-hidden flex items-center justify-center"
           style="background:#000; min-height: 240px;">
        <video #video autoplay playsinline muted class="w-full"
               style="max-height: 60vh; object-fit: contain;"></video>
        @if (starting()) {
          <div class="absolute inset-0 flex items-center justify-center text-white small">
            <span class="spinner-border spinner-border-sm me-2"></span>正在開啟鏡頭…
          </div>
        }
      </div>
      <div class="text-muted small mt-2">
        發票請平放、填滿畫面並保持光線充足，字跡清楚才辨識得出來；可連續拍多張，拍完按「完成」。
      </div>

      @if (shots().length > 0) {
        <div class="flex flex-wrap gap-2 mt-3">
          @for (s of shots(); track s.uid) {
            <div class="relative border rounded overflow-hidden" style="width: 88px; height: 88px;">
              <img [src]="s.url" alt="已拍攝" class="w-full h-full" style="object-fit: cover;">
              <button type="button" class="btn btn-sm btn-ghost-danger inline-flex items-center absolute"
                      style="top: 2px; right: 2px; padding: 2px; background: rgba(255,255,255,.85);"
                      (click)="removeShot(s.uid)" title="移除">
                <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
              </button>
            </div>
          }
        </div>
      }
    </div>

    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="cancel()">取消</button>
      <button type="button" class="btn btn-outline-primary inline-flex items-center gap-1"
              [disabled]="!ready() || shots().length >= maxShots" (click)="capture()">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#camera"></use></svg>
        拍照
      </button>
      <button type="button" class="btn btn-primary" [disabled]="shots().length === 0" (click)="done()">
        完成（{{ shots().length }} 張）
      </button>
    </div>
  `,
})
export class WebcamCaptureModal implements AfterViewInit, OnDestroy {
  private activeModal = inject(NgbActiveModal);
  private video = viewChild.required<ElementRef<HTMLVideoElement>>('video');

  readonly maxShots = MAX_SHOTS;

  devices  = signal<MediaDeviceInfo[]>([]);
  deviceId = signal<string>('');
  starting = signal(true);
  ready    = signal(false);
  error    = signal('');
  shots    = signal<Shot[]>([]);

  private stream: MediaStream | null = null;
  private uidSeq = 0;
  private destroyed = false;

  ngAfterViewInit(): void {
    void this.start();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.stopStream();
    this.shots().forEach(s => URL.revokeObjectURL(s.url));
  }

  private async start(deviceId?: string): Promise<void> {
    this.stopStream();
    this.starting.set(true);
    this.ready.set(false);
    this.error.set('');

    if (!isWebcamSupported()) {
      this.fail('此瀏覽器不支援鏡頭擷取，請改用「點擊上傳」選擇檔案。');
      return;
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: false,
        video: {
          // 解析度盡量拉高：OCR 準確度幾乎取決於畫質
          width:  {ideal: 1920},
          height: {ideal: 1080},
          ...(deviceId ? {deviceId: {exact: deviceId}} : {facingMode: {ideal: 'environment'}}),
        },
      });
      // 等權限的期間使用者可能已經關掉視窗 —— 此時立刻釋放，不然鏡頭燈會一直亮
      if (this.destroyed) {
        stream.getTracks().forEach(t => t.stop());
        return;
      }
      this.stream = stream;
      const el = this.video().nativeElement;
      el.srcObject = stream;
      await el.play().catch(() => undefined);

      this.deviceId.set(stream.getVideoTracks()[0]?.getSettings().deviceId ?? deviceId ?? '');
      // 取得權限後 enumerateDevices 才拿得到鏡頭名稱
      const all = await navigator.mediaDevices.enumerateDevices();
      this.devices.set(all.filter(d => d.kind === 'videoinput'));

      this.starting.set(false);
      this.ready.set(true);
    } catch (e) {
      this.fail(this.describeError(e));
    }
  }

  private fail(message: string): void {
    this.starting.set(false);
    this.ready.set(false);
    this.error.set(message);
  }

  private describeError(e: unknown): string {
    const name = (e as DOMException)?.name ?? '';
    switch (name) {
      case 'NotAllowedError':
      case 'SecurityError':
        return '沒有鏡頭使用權限。請點網址列左側的圖示，將「相機」改為允許後重新開啟此視窗。';
      case 'NotFoundError':
      case 'OverconstrainedError':
        return '找不到可用的鏡頭，請確認鏡頭已連接。';
      case 'NotReadableError':
      case 'AbortError':
        return '鏡頭正被其他程式使用中（例如視訊會議），請關閉後再試一次。';
      default:
        return '無法開啟鏡頭，請改用「點擊上傳」選擇檔案。';
    }
  }

  switchDevice(id: string): void {
    if (id && id !== this.deviceId()) void this.start(id);
  }

  capture(): void {
    const el = this.video().nativeElement;
    if (!this.ready() || !el.videoWidth || this.shots().length >= MAX_SHOTS) return;

    const canvas = document.createElement('canvas');
    canvas.width  = el.videoWidth;
    canvas.height = el.videoHeight;
    canvas.getContext('2d')?.drawImage(el, 0, 0, canvas.width, canvas.height);

    canvas.toBlob(blob => {
      if (!blob) return;
      const uid  = this.uidSeq++;
      const file = new File([blob], `webcam-${this.timestamp()}-${uid + 1}.jpg`, {type: 'image/jpeg'});
      this.shots.update(list => [...list, {uid, file, url: URL.createObjectURL(file)}]);
    }, 'image/jpeg', 0.92);
  }

  removeShot(uid: number): void {
    const shot = this.shots().find(s => s.uid === uid);
    if (shot) URL.revokeObjectURL(shot.url);
    this.shots.update(list => list.filter(s => s.uid !== uid));
  }

  done(): void {
    const files = this.shots().map(s => s.file);
    this.stopStream();
    this.activeModal.close(files);
  }

  cancel(): void {
    this.stopStream();
    this.activeModal.dismiss();
  }

  private stopStream(): void {
    this.stream?.getTracks().forEach(t => t.stop());
    this.stream = null;
  }

  /** yyyyMMdd-HHmmss（本地時間，只用於檔名） */
  private timestamp(): string {
    const d = new Date();
    const p = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}-${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}`;
  }
}
