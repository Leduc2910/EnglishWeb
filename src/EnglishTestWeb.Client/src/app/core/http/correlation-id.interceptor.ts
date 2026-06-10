import { HttpInterceptorFn } from '@angular/common/http';
import { isApiRequest } from './api-request';

export const correlationIdInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiRequest(request.url)) {
    return next(request);
  }

  const correlationId = globalThis.crypto?.randomUUID?.() ?? crypto.randomUUID();

  return next(
    request.clone({
      setHeaders: {
        'X-Correlation-Id': correlationId,
      },
    }),
  );
};
