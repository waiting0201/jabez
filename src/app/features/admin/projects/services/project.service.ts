import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {Project} from '../models/project.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';

const MOCK_PROJECTS: Project[] = [
  {
    id:  1, code: 'P2024-001',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 500000,  actualAmount: 480000,  businessAmount: 450000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example1',
    createdAt: new Date('2024-03-01'),
  },
  {
    id:  2, code: 'P2024-002',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 1200000, actualAmount: 0,        businessAmount: 1100000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example2',
    createdAt: new Date('2024-06-15'),
  },
  {
    id:  3, code: 'P2025-001',
    departmentId: 1, departmentName: '管理部',
    budgetAmount: 300000,  actualAmount: 280000,  businessAmount: 250000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example3',
    createdAt: new Date('2025-01-10'),
  },
  {
    id:  4, code: 'P2024-003',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 750000,  actualAmount: 620000,  businessAmount: 700000,
    createdAt: new Date('2024-04-20'),
  },
  {
    id:  5, code: 'P2024-004',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 980000,  actualAmount: 950000,  businessAmount: 900000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example5',
    createdAt: new Date('2024-05-08'),
  },
  {
    id:  6, code: 'P2024-005',
    departmentId: 4, departmentName: '財務部',
    budgetAmount: 420000,  actualAmount: 390000,  businessAmount: 380000,
    createdAt: new Date('2024-07-22'),
  },
  {
    id:  7, code: 'P2024-006',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 1500000, actualAmount: 1200000, businessAmount: 1400000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example7',
    createdAt: new Date('2024-08-05'),
  },
  {
    id:  8, code: 'P2024-007',
    departmentId: 1, departmentName: '管理部',
    budgetAmount: 200000,  actualAmount: 180000,  businessAmount: 160000,
    createdAt: new Date('2024-09-12'),
  },
  {
    id:  9, code: 'P2024-008',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 650000,  actualAmount: 600000,  businessAmount: 580000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example9',
    createdAt: new Date('2024-10-01'),
  },
  {
    id: 10, code: 'P2024-009',
    departmentId: 4, departmentName: '財務部',
    budgetAmount: 880000,  actualAmount: 0,        businessAmount: 850000,
    createdAt: new Date('2024-11-18'),
  },
  {
    id: 11, code: 'P2024-010',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 340000,  actualAmount: 320000,  businessAmount: 310000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example11',
    createdAt: new Date('2024-12-03'),
  },
  {
    id: 12, code: 'P2025-002',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 1600000, actualAmount: 800000,  businessAmount: 1500000,
    createdAt: new Date('2025-02-14'),
  },
  {
    id: 13, code: 'P2025-003',
    departmentId: 1, departmentName: '管理部',
    budgetAmount: 450000,  actualAmount: 440000,  businessAmount: 420000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example13',
    createdAt: new Date('2025-03-07'),
  },
  {
    id: 14, code: 'P2025-004',
    departmentId: 4, departmentName: '財務部',
    budgetAmount: 720000,  actualAmount: 680000,  businessAmount: 700000,
    createdAt: new Date('2025-04-22'),
  },
  {
    id: 15, code: 'P2025-005',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 290000,  actualAmount: 260000,  businessAmount: 270000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example15',
    createdAt: new Date('2025-05-30'),
  },
  {
    id: 16, code: 'P2025-006',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 1100000, actualAmount: 0,        businessAmount: 1050000,
    createdAt: new Date('2025-06-11'),
  },
  {
    id: 17, code: 'P2025-007',
    departmentId: 1, departmentName: '管理部',
    budgetAmount: 560000,  actualAmount: 530000,  businessAmount: 510000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example17',
    createdAt: new Date('2025-07-04'),
  },
  {
    id: 18, code: 'P2025-008',
    departmentId: 4, departmentName: '財務部',
    budgetAmount: 830000,  actualAmount: 790000,  businessAmount: 800000,
    createdAt: new Date('2025-08-19'),
  },
  {
    id: 19, code: 'P2025-009',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 410000,  actualAmount: 380000,  businessAmount: 390000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example19',
    createdAt: new Date('2025-09-25'),
  },
  {
    id: 20, code: 'P2025-010',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 1350000, actualAmount: 1100000, businessAmount: 1300000,
    createdAt: new Date('2025-10-08'),
  },
  {
    id: 21, code: 'P2025-011',
    departmentId: 1, departmentName: '管理部',
    budgetAmount: 670000,  actualAmount: 650000,  businessAmount: 630000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example21',
    createdAt: new Date('2025-11-15'),
  },
  {
    id: 22, code: 'P2026-001',
    departmentId: 4, departmentName: '財務部',
    budgetAmount: 920000,  actualAmount: 0,        businessAmount: 880000,
    createdAt: new Date('2026-01-06'),
  },
  {
    id: 23, code: 'P2026-002',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 580000,  actualAmount: 200000,  businessAmount: 550000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example23',
    createdAt: new Date('2026-02-01'),
  },
];

@Injectable({providedIn: 'root'})
export class ProjectService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<Project[]>(MOCK_PROJECTS);

  getAll(): Observable<Project[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<Project[]>('/api/projects').pipe(
    //   tap(projects => this.items$.next(projects)),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    return this.items$.asObservable();
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<Project>> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<{success: boolean; data: PagedResult<Project>}>(
    //   '/api/projects', {params: {page, pageSize}}
    // ).pipe(map(r => r.data!));

    // ─── Mock（前端開發用）──────────────────────────────────
    const all = this.items$.getValue();
    const start = (page - 1) * pageSize;
    const totalPages = Math.max(1, Math.ceil(all.length / pageSize));
    return of({items: all.slice(start, start + pageSize), totalCount: all.length, page, pageSize, totalPages});
  }

  getById(id: number): Observable<Project | undefined> {
    // return this.http.get<Project>(`/api/projects/${id}`);
    return of(this.items$.getValue().find(p => p.id === id));
  }

  create(data: Omit<Project, 'id' | 'createdAt'>): Observable<Project> {
    // return this.http.post<Project>('/api/projects', data).pipe(
    //   tap(p => this.items$.next([...this.items$.getValue(), p])),
    // );
    const item: Project = {...data, id: Date.now(), createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<Project>): Observable<Project> {
    // return this.http.patch<Project>(`/api/projects/${id}`, changes).pipe(
    //   tap(updated => {
    //     const list = this.items$.getValue().map(p => p.id === id ? updated : p);
    //     this.items$.next(list);
    //   }),
    // );
    const updated = this.items$.getValue().map(p => p.id === id ? {...p, ...changes} : p);
    this.items$.next(updated);
    return of(updated.find(p => p.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/projects/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(p => p.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(p => p.id !== id));
    return of(undefined);
  }
}
