import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {Project} from '../models/project.model';

const MOCK_PROJECTS: Project[] = [
  {
    id: 1, code: 'P2024-001',
    departmentId: 3, departmentName: '業務部',
    budgetAmount: 500000, actualAmount: 480000, businessAmount: 450000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example1',
    createdAt: new Date('2024-03-01'),
  },
  {
    id: 2, code: 'P2024-002',
    departmentId: 2, departmentName: '資訊部',
    budgetAmount: 1200000, actualAmount: 0, businessAmount: 1100000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example2',
    createdAt: new Date('2024-06-15'),
  },
  {
    id: 3, code: 'P2025-001',
    departmentId: 1, departmentName: '管理部',
    budgetAmount: 300000, actualAmount: 280000, businessAmount: 250000,
    googleDriveUrl: 'https://drive.google.com/drive/folders/example3',
    createdAt: new Date('2025-01-10'),
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
