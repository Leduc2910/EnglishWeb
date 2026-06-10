import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { correlationIdInterceptor } from './correlation-id.interceptor';
import { credentialsInterceptor } from './credentials.interceptor';
import { problemDetailsInterceptor } from './problem-details.interceptor';

describe('HTTP interceptors', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([
            credentialsInterceptor,
            correlationIdInterceptor,
            problemDetailsInterceptor,
          ]),
        ),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends withCredentials and correlation id for /api requests', () => {
    http.get('/api/health').subscribe();

    const request = httpMock.expectOne('/api/health');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-Correlation-Id')).toBeTruthy();
    request.flush({ status: 'ok' });
  });

  it('does not attach API headers for non-api requests', () => {
    http.get('/assets/config.json').subscribe();

    const request = httpMock.expectOne('/assets/config.json');
    expect(request.request.withCredentials).toBe(false);
    expect(request.request.headers.has('X-Correlation-Id')).toBe(false);
    request.flush({});
  });

  it('maps API problem details errors to ApiProblemError', () => {
    let capturedCode: string | undefined;

    http.get('/api/health').subscribe({
      error: (error: { code?: string }) => {
        capturedCode = error.code;
      },
    });

    const request = httpMock.expectOne('/api/health');
    request.flush(
      { title: 'XSRF token is required.', extensions: { code: 'auth.xsrfRequired' } },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(capturedCode).toBe('auth.xsrfRequired');
  });
});
