import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, throwError } from 'rxjs';
import { AuthApiService } from '../auth/auth-api.service';
import { isApiRequest } from './api-request';
import { XsrfTokenStore } from './xsrf-token.store';

function isUnsafeMethod(method: string): boolean {
  return method === 'POST' || method === 'PUT' || method === 'PATCH' || method === 'DELETE';
}

export const xsrfHeaderInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiRequest(request.url) || !isUnsafeMethod(request.method)) {
    return next(request);
  }

  const store = inject(XsrfTokenStore);
  const authApi = inject(AuthApiService);

  const sendWithToken = (token: string) =>
    next(
      request.clone({
        setHeaders: {
          'X-XSRF-TOKEN': token,
        },
      }),
    );

  const existing = store.token();
  if (existing) {
    return sendWithToken(existing);
  }

  return from(authApi.issueXsrfToken()).pipe(
    switchMap(() => {
      const token = store.token();
      if (!token) {
        return throwError(() => new Error('XSRF token unavailable'));
      }

      return sendWithToken(token);
    }),
  );
};
