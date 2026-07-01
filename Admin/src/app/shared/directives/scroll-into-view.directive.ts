import {AfterViewInit, Directive, ElementRef, inject} from '@angular/core';

/**
 * 元素掛載時捲動到可視範圍。用於固定顯示在頁首的驗證錯誤訊息（errorMsg alert），
 * 避免表單很長時使用者捲到底送出、訊息跳在頂部卻看不到而誤以為「按了沒反應」。
 */
@Directive({
  selector: '[appScrollIntoView]',
})
export class ScrollIntoViewDirective implements AfterViewInit {
  private el = inject(ElementRef<HTMLElement>);

  ngAfterViewInit() {
    this.el.nativeElement.scrollIntoView({behavior: 'smooth', block: 'start'});
  }
}
