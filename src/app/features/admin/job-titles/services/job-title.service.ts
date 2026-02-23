import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {JobTitle} from '../models/job-title.model';

const MOCK_JOB_TITLES: JobTitle[] = [
  {id: 1, name: '工程師',     level: 1, employeeCount: 1, createdAt: new Date('2024-01-01')},
  {id: 2, name: '資深工程師', level: 2, employeeCount: 1, createdAt: new Date('2024-01-01')},
  {id: 3, name: '主任工程師', level: 3, employeeCount: 0, createdAt: new Date('2024-01-01')},
  {id: 4, name: '部門主管',   level: 4, employeeCount: 1, createdAt: new Date('2024-01-01')},
];

@Injectable({providedIn: 'root'})
export class JobTitleService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<JobTitle[]>(MOCK_JOB_TITLES);

  getAll(): Observable<JobTitle[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<JobTitle[]>('/api/job-titles').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getById(id: number): Observable<JobTitle | undefined> {
    // return this.http.get<JobTitle>(`/api/job-titles/${id}`);
    return of(this.items$.getValue().find(j => j.id === id));
  }

  create(data: Omit<JobTitle, 'id' | 'createdAt' | 'employeeCount'>): Observable<JobTitle> {
    // return this.http.post<JobTitle>('/api/job-titles', data).pipe(
    //   tap(j => this.items$.next([...this.items$.getValue(), j])),
    // );
    const item: JobTitle = {...data, id: Date.now(), employeeCount: 0, createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<JobTitle>): Observable<JobTitle> {
    // return this.http.patch<JobTitle>(`/api/job-titles/${id}`, changes).pipe(
    //   tap(updated => {
    //     const list = this.items$.getValue().map(j => j.id === id ? updated : j);
    //     this.items$.next(list);
    //   }),
    // );
    const updated = this.items$.getValue().map(j => j.id === id ? {...j, ...changes} : j);
    this.items$.next(updated);
    return of(updated.find(j => j.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/job-titles/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(j => j.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(j => j.id !== id));
    return of(undefined);
  }
}
