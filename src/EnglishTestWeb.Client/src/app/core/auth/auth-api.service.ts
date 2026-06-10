import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CurrentUser, LoginRequest } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  issueXsrfToken(): Promise<void> {
    return firstValueFrom(this.http.get('/api/security/xsrf-token')).then(() => undefined);
  }

  login(request: LoginRequest): Promise<CurrentUser> {
    return firstValueFrom(this.http.post<CurrentUser>('/api/auth/login', request));
  }

  logout(): Promise<void> {
    return firstValueFrom(this.http.post('/api/auth/logout', null)).then(() => undefined);
  }

  getCurrentUser(): Promise<CurrentUser> {
    return firstValueFrom(this.http.get<CurrentUser>('/api/auth/me'));
  }
}
