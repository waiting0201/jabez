import {Component, OnInit, inject, signal, computed} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {CalendarDayService} from '../../services/calendar-day.service';
import {CalendarDay} from '../../models/calendar-day.model';
import {ToastrService} from 'ngx-toastr';
import {DatePipe} from '@angular/common';
import {AuthService} from '@core/auth/services/auth.service';

/** 星期對照表（台灣中文） */
const WEEKDAY_LABELS = ['日', '一', '二', '三', '四', '五', '六'];

@Component({
  selector: 'app-calendar-day-list',
  templateUrl: './calendar-day-list.html',
  imports: [FormsModule, DatePipe],
})
export class CalendarDayList implements OnInit {
  private svc = inject(CalendarDayService);
  private toastr = inject(ToastrService);
  private auth = inject(AuthService);

  canWrite()  { return this.auth.hasPermission('calendar-days:write'); }
  canDelete() { return this.auth.hasPermission('calendar-days:delete'); }

  /** 目前選擇的年份，預設今年 */
  selectedYear = signal<number>(new Date().getFullYear());

  /** 選單可用的年份範圍（前後 3 年） */
  yearOptions = computed<number[]>(() => {
    const current = new Date().getFullYear();
    return Array.from({length: 7}, (_, i) => current - 3 + i);
  });

  /** 所有日曆資料（來自 API） */
  days = signal<CalendarDay[]>([]);

  /** 載入中狀態 */
  loading = signal<boolean>(false);

  /** 匯入中狀態 */
  importing = signal<boolean>(false);

  /** 行內編輯中的記錄 id（同時只允許一筆） */
  editingId = signal<number | null>(null);

  /** 行內編輯暫存值 */
  editingIsHoliday = signal<boolean>(false);
  editingDescription = signal<string>('');

  ngOnInit(): void {
    this.loadData();
  }

  /** 切換年份時重新載入 */
  onYearChange(year: number): void {
    this.selectedYear.set(Number(year));
    this.cancelEdit();
    this.loadData();
  }

  /** 取得星期中文標籤 */
  getWeekdayLabel(dateStr: string): string {
    const d = new Date(dateStr);
    return WEEKDAY_LABELS[d.getDay()];
  }

  /** 是否為週末（六日） */
  isWeekend(dateStr: string): boolean {
    const day = new Date(dateStr).getDay();
    return day === 0 || day === 6;
  }

  /** 載入該年行事曆資料 */
  loadData(): void {
    this.loading.set(true);
    this.svc.getByYear(this.selectedYear()).subscribe({
      next: (data) => {
        this.days.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.toastr.error('載入行事曆資料失敗');
        this.loading.set(false);
      },
    });
  }

  /** 從政府 API 匯入當年行事曆 */
  importYear(): void {
    const year = this.selectedYear();
    if (!confirm(`確定要匯入 ${year} 年行事曆資料嗎？\n若已有資料將會覆蓋。`)) return;

    this.importing.set(true);
    this.svc.importYear(year).subscribe({
      next: (data) => {
        this.days.set(data);
        this.toastr.success(`已匯入 ${year} 年行事曆，共 ${data.length} 筆`);
        this.importing.set(false);
      },
      error: () => {
        this.toastr.error('匯入失敗，請確認網路連線或稍後再試');
        this.importing.set(false);
      },
    });
  }

  /** 進入行內編輯模式 */
  startEdit(day: CalendarDay): void {
    this.editingId.set(day.id);
    this.editingIsHoliday.set(day.isHoliday);
    this.editingDescription.set(day.description ?? '');
  }

  /** 取消編輯 */
  cancelEdit(): void {
    this.editingId.set(null);
  }

  /** 儲存行內編輯 */
  saveEdit(day: CalendarDay): void {
    this.svc.update(day.id, {
      isHoliday: this.editingIsHoliday(),
      description: this.editingDescription(),
    }).subscribe({
      next: (updated) => {
        this.days.update(list =>
          list.map(d => d.id === updated.id ? updated : d)
        );
        this.toastr.success('更新成功');
        this.editingId.set(null);
      },
      error: () => this.toastr.error('更新失敗'),
    });
  }

  /** 刪除單筆記錄 */
  delete(day: CalendarDay): void {
    const label = new Date(day.date).toLocaleDateString('zh-TW');
    if (!confirm(`確定要刪除 ${label} 的行事曆資料嗎？`)) return;

    this.svc.delete(day.id).subscribe({
      next: () => {
        this.days.update(list => list.filter(d => d.id !== day.id));
        this.toastr.success('刪除成功');
      },
      error: () => this.toastr.error('刪除失敗'),
    });
  }
}
