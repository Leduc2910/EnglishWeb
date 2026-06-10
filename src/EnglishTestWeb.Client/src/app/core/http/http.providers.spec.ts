import { HttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { httpProviders } from './http.providers';
import { XsrfTokenStore } from './xsrf-token.store';

describe('httpProviders', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let xsrfTokenStore: XsrfTokenStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...httpProviders, provideHttpClientTesting()],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    xsrfTokenStore = TestBed.inject(XsrfTokenStore);
    xsrfTokenStore.setToken('xsrf-test-token');
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
