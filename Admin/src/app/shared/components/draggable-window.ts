import {ChangeDetectionStrategy, Component, ElementRef, HostListener, OnDestroy, inject, input, output, signal} from '@angular/core';

/** 浮動視窗的 z-index 起點：低於 ngb modal（1055）與檔案預覽（1060），讓預覽永遠疊在最上面 */
const BASE_Z = 1000;
/** 手機寬度門檻：以下退化為底部面板，不可拖拉 */
const MOBILE_MAX_WIDTH = 640;
/** 多視窗錯位位移（px） */
const CASCADE_STEP = 24;

/** 目前開啟中的視窗，順序即 z 軸順序（最後一個在最上） */
const registry: DraggableWindow[] = [];

/**
 * 可拖拉浮動視窗（無背景遮罩，可同時開多個）。
 * - 標題列為拖拉把手，右上 ✕ 關閉；拖拉時夾在 viewport 內
 * - 點視窗任何位置置頂；多視窗起始位置錯開 24px
 * - < 640px 退化為全寬底部面板、不可拖拉
 * - 內容以 ng-content 投影；關閉由父層 `@if` / `@for` 移除（emit closed）
 * 規範見 docs/frontend-design.md「可拖拉浮動視窗」。
 */
@Component({
  selector: 'app-draggable-window',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    :host { display: contents; }
    .dw {
      position: fixed;
      display: flex; flex-direction: column;
      width: min(560px, calc(100vw - 16px));
      max-height: min(70vh, 640px);
      background: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: 0.5rem;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.22);
      overflow: hidden;
    }
    .dw-mobile {
      left: 0 !important; right: 0; bottom: 0; top: auto !important;
      width: 100%; max-height: 70vh;
      border-radius: 0.75rem 0.75rem 0 0;
    }
    .dw-title {
      display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
      padding: 0.5rem 0.75rem;
      background: var(--forest); color: #fff;
      user-select: none; touch-action: none; cursor: grab;
    }
    .dw-title.dragging { cursor: grabbing; }
    .dw-mobile .dw-title { cursor: default; }
    .dw-close {
      flex: none; width: 1.75rem; height: 1.75rem; line-height: 1;
      border: 0; border-radius: 0.25rem; background: transparent; color: #fff; cursor: pointer;
    }
    .dw-close:hover { background: rgba(255, 255, 255, 0.2); }
    .dw-body { overflow: auto; padding: 0.75rem; flex: 1 1 auto; }
  `],
  template: `
    <div class="dw" [class.dw-mobile]="mobile()" role="dialog" [attr.aria-label]="title()"
         [style.left.px]="x()" [style.top.px]="y()" [style.z-index]="z()"
         (pointerdown)="bringToFront()">
      <div class="dw-title" [class.dragging]="dragging" (pointerdown)="onDown($event)"
           (pointermove)="onMove($event)" (pointerup)="onUp($event)" (pointercancel)="onUp($event)">
        <span class="fw-600 text-truncate">{{ title() }}</span>
        <button type="button" class="dw-close" aria-label="關閉" title="關閉"
                (pointerdown)="$event.stopPropagation()" (click)="closed.emit()">✕</button>
      </div>
      <div class="dw-body"><ng-content /></div>
    </div>
  `,
})
export class DraggableWindow implements OnDestroy {
  title  = input('');
  /** 識別用 key（父層用來判斷「是否已開啟」並置頂既有視窗） */
  key    = input<string | number | null>(null);
  closed = output<void>();

  x = signal(0);
  y = signal(0);
  z = signal(BASE_Z);
  mobile = signal(false);
  dragging = false;

  private el = inject<ElementRef<HTMLElement>>(ElementRef);
  private dragOrigin: {px: number; py: number; x: number; y: number} | null = null;

  constructor() {
    const n = registry.length;
    const off = (n % 8) * CASCADE_STEP;
    this.x.set(Math.max(8, Math.min(96 + off, window.innerWidth - 120)));
    this.y.set(Math.max(8, Math.min(88 + off, window.innerHeight - 120)));
    this.mobile.set(window.innerWidth < MOBILE_MAX_WIDTH);
    registry.push(this);
    this.renumber();
  }

  ngOnDestroy() {
    const i = registry.indexOf(this);
    if (i >= 0) registry.splice(i, 1);
    this.renumber();
  }

  /** 置頂：移到 registry 尾端並重排 z-index */
  bringToFront() {
    const i = registry.indexOf(this);
    if (i < 0 || i === registry.length - 1) return;
    registry.splice(i, 1);
    registry.push(this);
    this.renumber();
  }

  private renumber() {
    registry.forEach((w, i) => w.z.set(BASE_Z + i));
  }

  onDown(e: PointerEvent) {
    if (this.mobile() || e.button > 0) return;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    this.dragging = true;
    this.dragOrigin = {px: e.clientX, py: e.clientY, x: this.x(), y: this.y()};
  }

  onMove(e: PointerEvent) {
    if (!this.dragOrigin) return;
    this.setClamped(this.dragOrigin.x + e.clientX - this.dragOrigin.px,
                    this.dragOrigin.y + e.clientY - this.dragOrigin.py);
  }

  onUp(e: PointerEvent) {
    if (!this.dragOrigin) return;
    (e.currentTarget as HTMLElement).releasePointerCapture?.(e.pointerId);
    this.dragOrigin = null;
    this.dragging = false;
  }

  @HostListener('window:resize')
  onResize() {
    this.mobile.set(window.innerWidth < MOBILE_MAX_WIDTH);
    this.setClamped(this.x(), this.y());
  }

  /** 夾在 viewport 內：標題列永遠留在畫面中，才拖得回來 */
  private setClamped(x: number, y: number) {
    const w = this.el.nativeElement.firstElementChild as HTMLElement | null;
    const width = w?.offsetWidth ?? 560;
    this.x.set(Math.max(0, Math.min(x, window.innerWidth - width)));
    this.y.set(Math.max(0, Math.min(y, window.innerHeight - 44)));
  }
}
