import { Injectable, computed, inject, signal } from '@angular/core';
import { API_AUTH_ERROR_MESSAGES, LOGIN_ERROR_MESSAGES } from './auth.models';
import { AuthApiService } from './auth-api.service';
import { CurrentUser, LoginRequest } from './auth.models';
import { readProblemCode } from '../http/problem-details';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly authApi = inject(AuthApiService);
  private readonly currentUserSignal = signal<CurrentUser | null>(null);
  private sessionLoaded = false;
  private sessionLoadPromise: Promise<CurrentUser | null> | null = null;

  readonly usesBrowserTokenStorage = false;
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly isTeacher = computed(() =>
    this.currentUserSignal()?.roles.includes('Teacher') ?? false,
  );

  persistAccessToken(_token: string): void {
    throw new Error('Browser token storage is disabled for EnglishTestWeb.');
  }

  async ensureSessionLoaded(): Promise<CurrentUser | null> {
    if (this.sessionLoaded) {
      return this.currentUserSignal();
    }

    this.sessionLoadPromise ??= this.loadSessionInternal();
    return this.sessionLoadPromise;
  }

  async loadSession(): Promise<CurrentUser | null> {
    this.sessionLoadPromise = this.loadSessionInternal();
    return this.sessionLoadPromise;
  }

  async login(request: LoginRequest): Promise<CurrentUser> {
    await this.authApi.issueXsrfToken();
    const user = await this.authApi.login(request);
    this.currentUserSignal.set(user);
    this.sessionLoaded = true;
    this.sessionLoadPromise = Promise.resolve(user);
    return user;
  }

  async logout(): Promise<void> {
    await this.authApi.issueXsrfToken();
    try {
      await this.authApi.logout();
    } catch {
      // Best-effort server sign-out; client session is always cleared in finally.
    } finally {
      this.clearSession();
    }
  }

  mapApiError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error;
      if (body && typeof body === 'object') {
        const code = readProblemCode(body as { code?: string; extensions?: { code?: string } });
        if (code && API_AUTH_ERROR_MESSAGES[code]) {
          return API_AUTH_ERROR_MESSAGES[code];
        }
      }

      if (error.status === 0) {
        return LOGIN_ERROR_MESSAGES['ERR_LOGIN_NETWORK'];
      }
    }

    return LOGIN_ERROR_MESSAGES['ERR_LOGIN_INVALID'];
  }

  clearSession(): void {
    this.currentUserSignal.set(null);
    this.sessionLoaded = true;
    this.sessionLoadPromise = Promise.resolve(null);
  }

  private async loadSessionInternal(): Promise<CurrentUser | null> {
    try {
      const user = await this.authApi.getCurrentUser();
      this.currentUserSignal.set(user);
      return user;
    } catch {
      this.currentUserSignal.set(null);
      return null;
    } finally {
      this.sessionLoaded = true;
    }
  }
}
