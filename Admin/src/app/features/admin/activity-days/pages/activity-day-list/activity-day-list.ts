import {Component, OnInit, computed, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
import {environment} from '@/environments/environment';
import {AuthService} from '@core/auth/services/auth.service';
import {ActivityDayService} from '../../services/activity-day.service';
import {ActivityDay, AffectedSchedule} from '../../models/activity-day.model';

interface DeptLookup { id: number; name: string; parentId: number | null; }
interface UserLookupRow { id: string; name: string; departmentId?: number; status: string; }

/**
 * 活動日管理（四週彈性工時 §3.2）—— 各部門協理於活動 2 個月前排定日期並勾選預定人力。
 *
 * 三件事在畫面上要講清楚：
 * 1. **活動日可以排在國定假日上**（預定人力當天直接打上下班卡、不需加班單），列表以 badge 標示
 * 2. **改期後系統不自動改寫個人班表** —— 只回報「重跑檢核不通過」的同仁，由主管通知其送〈改班申請〉
 * 3. 排定範圍受部門可見性限制，下拉只列得到自己看得到的部門（後端另有把關）
 */
@Component({
  selector: 'app-activity-day-list',
  templateUrl: './activity-day-list.html',
  imports: [CommonModule, FormsModule],
})
export class ActivityDayList implements OnInit {
  private svc = inject(ActivityDayService);
  private http = inject(HttpClient);
  private toastr = inject(ToastrService);
  private auth = inject(AuthService);

  canWrite = this.auth.hasPermission('activity-days:write');

  year = signal(new Date().getFullYear());
  month = signal(new Date().getMonth() + 1);

  loading = signal(false);
  saving = signal(false);
  items = signal<ActivityDay[]>([]);
  departments = signal<DeptLookup[]>([]);
  users = signal<UserLookupRow[]>([]);

  /** 編輯中的活動日 id；0 ＝ 新增；null ＝ 表單關閉 */
  editingId = signal<number | null>(null);
  form = signal<{date: string; departmentId: number | null; title: string; assigneeUserIds: string[]}>(
    {date: '', departmentId: null, title: '', assigneeUserIds: []});

  /** 改期後排班不合規的同仁，需由主管通知他們送〈改班申請〉 */
  affected = signal<AffectedSchedule[]>([]);

  readonly yearOptions = computed(() => {
    const y = new Date().getFullYear();
    return [y - 1, y, y + 1, y + 2];
  });
  readonly monthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  /** 預定人力候選：所選部門的在職同仁；未選部門時列全部看得到的人 */
  readonly candidates = computed(() => {
    const deptId = this.form().departmentId;
    return this.users().filter(u => !deptId || u.departmentId === deptId);
  });

  ngOnInit(): void {
    this.http.get<DeptLookup[]>(`${environment.apiUrl}/departments/lookup`)
      .subscribe({next: d => this.departments.set(d ?? []), error: () => {}});
    this.http.get<UserLookupRow[]>(`${environment.apiUrl}/users/lookup?scope=department`)
      .subscribe({next: u => this.users.set((u ?? []).filter(x => x.status === 'active')), error: () => {}});
    this.load();
  }

  /** 切換年／月：換月才清掉上一次的受影響清單（存檔後的 load 不可清，否則警示區塊會被自己洗掉）。 */
  onPeriodChange(): void {
    this.affected.set([]);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.svc.getByMonth(this.year(), this.month()).subscribe({
      next: rows => { this.items.set(rows ?? []); this.loading.set(false); },
      error: err => { this.toastr.error(err?.error?.message ?? '載入活動日失敗'); this.loading.set(false); },
    });
  }

  startCreate(): void {
    this.affected.set([]);
    this.editingId.set(0);
    this.form.set({
      date: `${this.year()}-${String(this.month()).padStart(2, '0')}-01`,
      departmentId: this.departments()[0]?.id ?? null,
      title: '',
      assigneeUserIds: [],
    });
  }

  startEdit(a: ActivityDay): void {
    this.affected.set([]);
    this.editingId.set(a.id);
    this.form.set({
      date: a.date.slice(0, 10),
      departmentId: a.departmentId,
      title: a.title,
      assigneeUserIds: a.assignees.map(x => x.userId),
    });
  }

  cancel(): void {
    this.editingId.set(null);
  }

  /**
   * 換部門要清空已選人力（候選名單會整組換掉），但**必須擋掉「值沒真的變」的那次觸發** ——
   * 編輯表單載入時 select 初始化也會 emit ngModelChange，
   * 無條件清空會把回填的預定人力洗掉，而畫面上完全看不出來（名單就是空的）。
   */
  onDepartmentChange(value: unknown): void {
    const id = Number(value);
    const f = this.form();
    if (f.departmentId === id) return;
    this.form.set({...f, departmentId: id, assigneeUserIds: []});
  }

  toggleAssignee(userId: string): void {
    this.form.update(f => ({
      ...f,
      assigneeUserIds: f.assigneeUserIds.includes(userId)
        ? f.assigneeUserIds.filter(x => x !== userId)
        : [...f.assigneeUserIds, userId],
    }));
  }

  isAssigned(userId: string): boolean {
    return this.form().assigneeUserIds.includes(userId);
  }

  save(): void {
    if (this.saving()) return;      // in-flight 鎖
    const f = this.form();

    if (!f.date)          { this.toastr.warning('請選擇活動日期'); return; }
    if (!f.departmentId)  { this.toastr.warning('請選擇部門'); return; }
    if (!f.title.trim())  { this.toastr.warning('請填寫活動名稱'); return; }

    this.saving.set(true);
    const payload = {
      date: f.date,
      departmentId: f.departmentId,
      title: f.title.trim(),
      assigneeUserIds: f.assigneeUserIds,
    };
    const id = this.editingId();
    const req$ = id && id > 0 ? this.svc.update(id, payload) : this.svc.create(payload);

    req$.subscribe({
      next: res => {
        this.saving.set(false);
        this.editingId.set(null);
        this.affected.set(res?.affected ?? []);
        this.toastr.success(res?.dateChanged ? '活動日已改期' : '活動日已儲存');
        if ((res?.affected?.length ?? 0) > 0) {
          this.toastr.warning(`有 ${res.affected.length} 位同仁的班表因此不符規定，請通知他們提出〈改班申請〉`);
        }
        this.load();
      },
      error: err => {
        this.saving.set(false);
        this.toastr.error(err?.error?.message ?? '活動日儲存失敗');
      },
    });
  }

  remove(a: ActivityDay): void {
    if (!confirm(`確定刪除「${a.title}」（${a.date.slice(0, 10)}）？`)) return;
    this.svc.remove(a.id).subscribe({
      next: () => { this.toastr.success('活動日已刪除'); this.load(); },
      error: err => this.toastr.error(err?.error?.message ?? '刪除失敗'),
    });
  }

  /** 模板不支援箭頭函式，名單在此組好。 */
  assigneeNames(a: ActivityDay): string {
    return a.assignees.map(x => x.userName).join('、');
  }

  deptName(id: number | null): string {
    return this.departments().find(d => d.id === id)?.name ?? '';
  }
}
