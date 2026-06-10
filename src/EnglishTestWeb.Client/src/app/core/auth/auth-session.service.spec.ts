import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  let localStorageSetItem: ReturnType<typeof vi.spyOn>;
  let sessionStorageSetItem: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthSessionService);
    localStorageSetItem = vi.spyOn(Storage.prototype, 'setItem');
    sessionStorageSetItem = vi.spyOn(Storage.prototype, 'setItem');
  });

  afterEach(() => {
    localStorageSetItem.mockRestore();
    sessionStorageSetItem.mockRestore();
  });

  it('does not use browser token storage', () => {
    expect(service.usesBrowserTokenStorage).toBe(false);
  });

  it('rejects persisting access tokens in browser storage', () => {
    expect(() => service.persistAccessToken('token')).toThrow(
      'Browser token storage is disabled for EnglishTestWeb.',
    );
    expect(localStorage.setItem).not.toHaveBeenCalled();
    expect(sessionStorage.setItem).not.toHaveBeenCalled();
  });
});
