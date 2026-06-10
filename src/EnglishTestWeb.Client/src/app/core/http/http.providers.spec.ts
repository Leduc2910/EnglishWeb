import { HttpClient, HttpXsrfTokenExtractor } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpProviders } from './http.providers';

class TestXsrfTokenExtractor implements HttpXsrfTokenExtractor {
  getToken(): string | null {
    return 'xsrf-test-token';
  }
}

describe('httpProviders', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ...httpProviders,
        provideHttpClientTesting(),
        { provide: HttpXsrfTokenExtractor, useClass: TestXsrfTokenExtractor },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends the configured XSRF header on unsafe API requests', () => {
    http.post('/api/health/unsafe-smoke', {}).subscribe();

    const request = httpMock.expectOne('/api/health/unsafe-smoke');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-test-token');
    expect(request.request.withCredentials).toBe(true);
    request.flush({ status: 'ok' });
  });
});
