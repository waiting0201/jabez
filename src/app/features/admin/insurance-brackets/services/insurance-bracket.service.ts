import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {InsuranceBracket} from '../models/insurance-bracket.model';
import {environment} from '../../../../../environments/environment';

@Injectable({providedIn: 'root'})
export class InsuranceBracketService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/insurance-brackets`;

  getAll(): Observable<InsuranceBracket[]> {
    return this.http.get<InsuranceBracket[]>(this.base);
  }

  getById(id: number): Observable<InsuranceBracket> {
    return this.http.get<InsuranceBracket>(`${this.base}/${id}`);
  }

  create(data: Omit<InsuranceBracket, 'id' | 'createdAt'>): Observable<InsuranceBracket> {
    return this.http.post<InsuranceBracket>(this.base, data);
  }

  update(id: number, changes: Partial<InsuranceBracket>): Observable<InsuranceBracket> {
    return this.http.patch<InsuranceBracket>(`${this.base}/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
