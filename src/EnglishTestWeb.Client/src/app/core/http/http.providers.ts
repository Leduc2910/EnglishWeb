import {
  provideHttpClient,
  withInterceptors,
  withNoXsrfProtection,
} from '@angular/common/http';
import { correlationIdInterceptor } from './correlation-id.interceptor';
import { credentialsInterceptor } from './credentials.interceptor';
import { problemDetailsInterceptor } from './problem-details.interceptor';
import { xsrfHeaderInterceptor } from './xsrf-header.interceptor';

export const httpProviders = [
  provideHttpClient(
    withNoXsrfProtection(),
    withInterceptors([
      credentialsInterceptor,
      xsrfHeaderInterceptor,
      correlationIdInterceptor,
      problemDetailsInterceptor,
    ]),
  ),
];
