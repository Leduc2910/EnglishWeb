import { HttpInterceptorFn } from '@angular/common/http';

export const correlationIdInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api')) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        'X-Correlation-Id': crypto.randomUUID(),
      },
    }),
  );
};
