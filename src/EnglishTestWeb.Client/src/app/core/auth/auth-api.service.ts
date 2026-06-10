import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CurrentUser, LoginRequest, StudentLoginRequest, StudentLoginResponse } from './auth.models';
import { XsrfTokenStore } from '../http/xsrf-token.store';

interface XsrfTokenResponse {
  cookieName: string;
  headerName: string;
  requestToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly xsrfTokenStore = inject(XsrfTokenStore);
  private issueXsrfTokenPromise: Promise<void> | null = null;

  issueXsrfToken(): Promise<void> {
    if (!this.issueXsrfTokenPromise) {
      this.issueXsrfTokenPromise = firstValueFrom(
        this.http.get<XsrfTokenResponse>('/api/security/xsrf-token'),
      )
        .then((response) => {
          this.xsrfTokenStore.setToken(response.requestToken);
        })
        .finally(() => {
          this.issueXsrfTokenPromise = null;
        });
    }

    return this.issueXsrfTokenPromise;
  }

  login(request: LoginRequest): Promise<CurrentUser> {
    return firstValueFrom(this.http.post<CurrentUser>('/api/auth/login', request));
  }

  loginStudent(request: StudentLoginRequest): Promise<StudentLoginResponse> {
    return firstValueFrom(this.http.post<StudentLoginResponse>('/api/auth/student/login', request));
  }

  logout(): Promise<void> {
    return firstValueFrom(this.http.post('/api/auth/logout', null)).then(() => undefined);
  }

  getCurrentUser(): Promise<CurrentUser> {
    return firstValueFrom(this.http.get<CurrentUser>('/api/auth/me'));
  }
}
