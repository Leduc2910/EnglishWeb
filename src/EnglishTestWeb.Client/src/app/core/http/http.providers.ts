import {
  provideHttpClient,
  withFetch,
  withInterceptors,
  withXsrfConfiguration,
} from '@angular/common/http';
import { correlationIdInterceptor } from './correlation-id.interceptor';
import { credentialsInterceptor } from './credentials.interceptor';
import { problemDetailsInterceptor } from './problem-details.interceptor';

export const httpProviders = [
  provideHttpClient(
    withFetch(),
    withXsrfConfiguration({
      cookieName: 'XSRF-TOKEN',
      headerName: 'X-XSRF-TOKEN',
    }),
    withInterceptors([
      credentialsInterceptor,
      correlationIdInterceptor,
      problemDetailsInterceptor,
    ]),
  ),
];
