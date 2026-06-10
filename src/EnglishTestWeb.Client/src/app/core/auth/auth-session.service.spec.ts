import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';
import { AuthApiService } from './auth-api.service';
import { ClassContextService } from '../classes/class-context.service';

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
  let classContext: ClassContextService;

  beforeEach(() => {
    authApi = {
      issueXsrfToken: vi.fn().mockResolvedValue(undefined),
      login: vi.fn(),
      logout: vi.fn().mockResolvedValue(undefined),
      getCurrentUser: vi.fn().mockRejectedValue(new Error('unauthorized')),
    };

    TestBed.configureTestingModule({
      providers: [AuthSessionService, ClassContextService, { provide: AuthApiService, useValue: authApi }],
    });
    classContext = TestBed.inject(ClassContextService);
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

  it('hydrates active class from /me for students', async () => {
    authApi.getCurrentUser.mockResolvedValue({
      userId: 'student-1',
      email: 'student@test.local',
      userName: 'student',
      roles: ['Student'],
      activeClass: {
        classId: 'class-1',
        className: 'English 7A',
        classCode: 'ENG7A',
      },
    });

    await service.loadSession();

    expect(classContext.activeClass()?.classCode).toBe('ENG7A');
  });

  it('clears stale client class context when /me omits activeClass', async () => {
    sessionStorage.setItem('EnglishTestWeb.ClassCode', 'ENG7A');
    sessionStorage.setItem('EnglishTestWeb.ClassConfirmed', 'ENG7A');
    classContext.setActiveClass({
      classId: 'stale-class',
      className: 'Stale Class',
      classCode: 'ENG7A',
    });

    authApi.getCurrentUser.mockResolvedValue({
      userId: 'student-1',
      email: 'student@test.local',
      userName: 'student',
      roles: ['Student'],
    });

    await service.loadSession();

    expect(classContext.activeClass()).toBeNull();
    expect(sessionStorage.getItem('EnglishTestWeb.ClassCode')).toBeNull();
  });

  it('clears stale client class context when authenticated user is not a student', async () => {
    sessionStorage.setItem('EnglishTestWeb.ClassCode', 'ENG7A');
    sessionStorage.setItem('EnglishTestWeb.ClassConfirmed', 'ENG7A');
    classContext.setActiveClass({
      classId: 'stale-class',
      className: 'Stale Class',
      classCode: 'ENG7A',
    });

    authApi.getCurrentUser.mockResolvedValue({
      userId: 'teacher-1',
      email: 'teacher@test.local',
      userName: 'teacher',
      roles: ['Teacher'],
    });

    await service.loadSession();

    expect(classContext.activeClass()).toBeNull();
    expect(sessionStorage.getItem('EnglishTestWeb.ClassCode')).toBeNull();
  });

  it('clears stale client class context when /me fails', async () => {
    sessionStorage.setItem('EnglishTestWeb.ClassCode', 'ENG7A');
    sessionStorage.setItem('EnglishTestWeb.ClassConfirmed', 'ENG7A');
    classContext.setActiveClass({
      classId: 'stale-class',
      className: 'Stale Class',
      classCode: 'ENG7A',
    });

    await service.loadSession();

    expect(classContext.activeClass()).toBeNull();
    expect(sessionStorage.getItem('EnglishTestWeb.ClassCode')).toBeNull();
  });
});
