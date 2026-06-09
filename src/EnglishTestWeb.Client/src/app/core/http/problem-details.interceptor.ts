import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiProblemDetails, readProblemCode } from './problem-details';

export class ApiProblemError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly problem: ApiProblemDetails;

  constructor(response: HttpErrorResponse, problem: ApiProblemDetails) {
    const code = readProblemCode(problem);
    super(code ?? problem.title ?? response.statusText);
    this.name = 'ApiProblemError';
    this.status = response.status;
    this.code = code;
    this.problem = problem;
  }
}

export const problemDetailsInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || !error.url?.includes('/api')) {
        return throwError(() => error);
      }

      const problem = error.error as ApiProblemDetails | null;
      if (!problem || typeof problem !== 'object') {
        return throwError(() => error);
      }

      return throwError(() => new ApiProblemError(error, problem));
    }),
  );
