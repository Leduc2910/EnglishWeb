import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';

describe('AuthSessionService', () => {
  let service: AuthSessionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthSessionService);
  });

  it('does not use browser token storage', () => {
    expect(service.usesBrowserTokenStorage).toBe(false);
  });

  it('rejects persisting access tokens in browser storage', () => {
    expect(() => service.persistAccessToken('token')).toThrowError(
      'Browser token storage is disabled for EnglishTestWeb.',
    );
  });
});
