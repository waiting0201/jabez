import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {map} from 'rxjs/operators';
import {ApprovalTask, ApprovalRecord, TaskStatus} from '../models/approval-task.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';

const MOCK_TASKS: ApprovalTask[] = [
  // ── Payment requests ──────────────────────────────────────────────────────
  {
    id: 2,
    applicationType: 'payment_request',
    title: '請款申請 #2（P2024-002）',
    submittedBy: '王小明',
    submittedAt: new Date('2024-07-05'),
    status: 'pending',
    currentStepOrder: 1,
    approvalRecords: [],
    flow: {
      id: 2, name: '採購申請',
      steps: [
        {stepOrder: 1, jobTitleName: '部門主管', note: '部門主管審核'},
        {stepOrder: 2, departmentName: '管理部', jobTitleName: '部門主管', note: '管理部核准'},
      ],
    },
    paymentDetail: {
      paymentRequestId: 2,
      paymentType: 'travel',
      projectCode: 'P2024-002',
      invoices: [
        {id: 'i3', fileName: 'receipt_hotel.jpg', invoiceNo: 'EF-11223344', amount: 4200},
        {id: 'i4', fileName: 'receipt_train.jpg', invoiceNo: 'GH-55667788', amount: 980},
      ],
      totalAmount: 5180,
    },
  },
  {
    id: 1,
    applicationType: 'payment_request',
    title: '請款申請 #1（P2024-001）',
    submittedBy: '李大華',
    submittedAt: new Date('2024-04-10'),
    status: 'approved',
    currentStepOrder: 2,
    reviewedAt: new Date('2024-04-12'),
    reviewNote: '金額核實無誤，已核准。',
    approvalRecords: [
      {stepOrder: 1, action: 'approved', reviewedBy: 'Alice', reviewedAt: new Date('2024-04-11')},
      {stepOrder: 2, action: 'approved', reviewedBy: 'Alice', reviewedAt: new Date('2024-04-12'), reviewNote: '金額核實無誤，已核准。'},
    ],
    paymentDetail: {
      paymentRequestId: 1,
      paymentType: 'vendor',
      projectCode: 'P2024-001',
      invoices: [
        {id: 'i1', fileName: 'invoice_001.jpg', invoiceNo: 'AB-12345678', amount: 15000},
        {id: 'i2', fileName: 'invoice_002.jpg', invoiceNo: 'CD-87654321', amount: 8500},
      ],
      totalAmount: 23500,
    },
  },
  // ── Leave requests ────────────────────────────────────────────────────────
  {
    id: 1,
    applicationType: 'leave',
    title: '請假申請 #1（annual）',
    submittedBy: 'Bob',
    submittedAt: new Date('2026-02-10'),
    status: 'pending',
    currentStepOrder: 1,
    approvalRecords: [],
    flow: {
      id: 1, name: '請假申請',
      steps: [
        {stepOrder: 1, departmentName: '管理部', jobTitleName: '部門主管', note: '直屬主管核可'},
        {stepOrder: 2, departmentName: '管理部', jobTitleName: '人資主管', note: '人事部門確認'},
      ],
    },
    leaveDetail: {
      leaveRequestId: 1,
      leaveType: 'annual',
      startDate: new Date('2026-03-01'),
      endDate:   new Date('2026-03-05'),
      days:      5,
      reason:    '個人旅遊',
    },
  },
  {
    id: 2,
    applicationType: 'leave',
    title: '請假申請 #2（sick）',
    submittedBy: 'Carol',
    submittedAt: new Date('2026-02-20'),
    status: 'approved',
    currentStepOrder: 2,
    reviewedAt: new Date('2026-02-20'),
    reviewNote: '核准',
    approvalRecords: [
      {stepOrder: 1, action: 'approved', reviewedBy: 'Alice', reviewedAt: new Date('2026-02-20'), reviewNote: '核准'},
      {stepOrder: 2, action: 'approved', reviewedBy: 'Alice', reviewedAt: new Date('2026-02-20'), reviewNote: '核准'},
    ],
    leaveDetail: {
      leaveRequestId: 2,
      leaveType: 'sick',
      startDate: new Date('2026-02-20'),
      endDate:   new Date('2026-02-21'),
      days:      2,
      reason:    '身體不適就醫',
    },
  },
  // ── Travel requests ───────────────────────────────────────────────────────
  {
    id: 1,
    applicationType: 'travel',
    title: '出差申請 #1（台南）',
    submittedBy: 'Alice',
    submittedAt: new Date('2026-03-01'),
    status: 'pending',
    currentStepOrder: 1,
    approvalRecords: [],
    travelDetail: {
      travelRequestId: 1,
      destination:   '台南',
      startDate:     new Date('2026-03-10'),
      endDate:       new Date('2026-03-12'),
      estimatedCost: 3000,
      purpose:       '客戶現場拜訪與需求確認',
      projectCode:   'P2024-001',
    },
  },
];

@Injectable({providedIn: 'root'})
export class ApprovalTaskService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<ApprovalTask[]>(MOCK_TASKS);

  pendingCount$ = this.items$.pipe(map(tasks => tasks.filter(t => t.status === 'pending').length));

  getAll(): Observable<ApprovalTask[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<ApprovalTask[]>('/api/approval-tasks').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<ApprovalTask>> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<{success: boolean; data: PagedResult<ApprovalTask>}>(
    //   '/api/approval-tasks', {params: {page, pageSize}}
    // ).pipe(map(r => r.data!));

    // ─── Mock（前端開發用）──────────────────────────────────
    const all = this.items$.getValue();
    const start = (page - 1) * pageSize;
    const totalPages = Math.max(1, Math.ceil(all.length / pageSize));
    return of({items: all.slice(start, start + pageSize), totalCount: all.length, page, pageSize, totalPages});
  }

  getById(id: number, applicationType?: string): Observable<ApprovalTask | undefined> {
    // return this.http.get<ApprovalTask>(`/api/approval-tasks/${id}`);
    return of(this.items$.getValue().find(
      t => t.id === id && (!applicationType || t.applicationType === applicationType)
    ));
  }

  review(id: number, applicationType: string, action: TaskStatus, reviewNote: string): Observable<ApprovalTask> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.patch<ApprovalTask>(
    //   `/api/approval-tasks/${applicationType}/${id}/review`,
    //   {action, reviewNote, applicationType},
    // ).pipe(
    //   tap(updated => this.items$.next(
    //     this.items$.getValue().map(t =>
    //       t.id === id && t.applicationType === applicationType ? updated : t
    //     )
    //   )),
    // );
    const current = this.items$.getValue().find(t => t.id === id && t.applicationType === applicationType)!;
    const record: ApprovalRecord = {
      stepOrder:   current.currentStepOrder,
      action:      action as 'approved' | 'returned' | 'rejected',
      reviewedBy:  'Alice（模擬）',
      reviewedAt:  new Date(),
      reviewNote:  reviewNote || undefined,
    };
    const totalSteps = current.flow?.steps.length ?? 0;

    let newStatus: TaskStatus = action;
    let newStep = current.currentStepOrder;

    if (action === 'approved' && totalSteps > 0 && current.currentStepOrder < totalSteps) {
      newStatus = 'pending'; // 進入下一步
      newStep   = current.currentStepOrder + 1;
    }

    const updated = this.items$.getValue().map(t =>
      t.id === id && t.applicationType === applicationType
        ? {
            ...t,
            status:           newStatus,
            currentStepOrder: newStep,
            reviewedAt:       new Date(),
            reviewNote:       reviewNote || undefined,
            approvalRecords:  [...t.approvalRecords, record],
          }
        : t
    );
    this.items$.next(updated);
    return of(updated.find(t => t.id === id && t.applicationType === applicationType)!);
  }
}
