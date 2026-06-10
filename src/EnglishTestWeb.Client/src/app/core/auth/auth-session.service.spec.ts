import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';
import { AuthApiService } from './auth-api.service';
import { ClassesApiService } from '../classes/classes-api.service';

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  let localStorageSetItem: ReturnType<typeof vi.spyOn>;
  let sessionStorageSetItem: ReturnType<typeof vi.spyOn>;
  let authApi: {
    issueXsrfToken: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    getCurrentUser: ReturnType<typeof vi.fn>;
  };
  let classesApi: {
    lookupByCode: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authApi = {
      issueXsrfToken: vi.fn().mockResolvedValue(undefined),
      login: vi.fn(),
      logout: vi.fn().mockResolvedValue(undefined),
      getCurrentUser: vi.fn().mockRejectedValue(new Error('unauthorized')),
    };
    classesApi = {
      lookupByCode: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthApiService, useValue: authApi },
        { provide: ClassesApiService, useValue: classesApi },
      ],
    });
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

  it('loads session without browser storage writes', async () => {
    await service.loadSession();
    expect(localStorage.setItem).not.toHaveBeenCalled();
    expect(sessionStorage.setItem).not.toHaveBeenCalled();
    expect(service.isAuthenticated()).toBe(false);
  });
});
