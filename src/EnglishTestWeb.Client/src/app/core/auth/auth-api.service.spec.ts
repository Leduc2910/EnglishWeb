import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthApiService } from './auth-api.service';
import { XsrfTokenStore } from '../http/xsrf-token.store';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;
  let xsrfTokenStore: XsrfTokenStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
    xsrfTokenStore = TestBed.inject(XsrfTokenStore);
    xsrfTokenStore.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deduplicates concurrent issueXsrfToken calls', async () => {
    const first = service.issueXsrfToken();
    const second = service.issueXsrfToken();

    expect(first).toBe(second);

    const request = httpMock.expectOne('/api/security/xsrf-token');
    request.flush({
      cookieName: 'XSRF-TOKEN',
      headerName: 'X-XSRF-TOKEN',
      requestToken: 'shared-xsrf-token',
    });

    await Promise.all([first, second]);
    expect(xsrfTokenStore.token()).toBe('shared-xsrf-token');
    httpMock.expectNone('/api/security/xsrf-token');
  });
});
