import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { studentGuard, studentLoginGuard } from './student.guard';
import { AuthSessionService } from '../auth/auth-session.service';
import { ClassContextService } from '../classes/class-context.service';

describe('studentGuard', () => {
  let router: Router;
  let auth: {
    ensureSessionLoaded: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
    isTeacher: ReturnType<typeof vi.fn>;
    isStudent: ReturnType<typeof vi.fn>;
  };
  let classContext: {
    activeClass: ReturnType<typeof vi.fn>;
    readPersistedClassCode: ReturnType<typeof vi.fn>;
  };

  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/student/tests' } as RouterStateSnapshot;

  beforeEach(() => {
    auth = {
      ensureSessionLoaded: vi.fn().mockResolvedValue(null),
      isAuthenticated: vi.fn().mockReturnValue(false),
      isTeacher: vi.fn().mockReturnValue(false),
      isStudent: vi.fn().mockReturnValue(false),
    };
    classContext = {
      activeClass: vi.fn().mockReturnValue(null),
      readPersistedClassCode: vi.fn().mockReturnValue(null),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: auth },
        { provide: ClassContextService, useValue: classContext },
      ],
    });

    router = TestBed.inject(Router);
  });

  it('redirects unauthenticated users to class entry', async () => {
    const result = await TestBed.runInInjectionContext(() => studentGuard(route, state));

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/class?returnUrl=%2Fstudent%2Ftests');
  });

  it('redirects authenticated students without active class to class entry', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.isStudent.mockReturnValue(true);
    classContext.activeClass.mockReturnValue(null);
    classContext.readPersistedClassCode.mockReturnValue('ENG7A');

    const result = await TestBed.runInInjectionContext(() => studentGuard(route, state));

    expect(router.serializeUrl(result as UrlTree)).toBe('/class');
  });
});

describe('studentLoginGuard', () => {
  let router: Router;
  let auth: {
    ensureSessionLoaded: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
    isTeacher: ReturnType<typeof vi.fn>;
    isStudent: ReturnType<typeof vi.fn>;
  };
  let classContext: {
    readPersistedClassCode: ReturnType<typeof vi.fn>;
    isConfirmedForClass: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    auth = {
      ensureSessionLoaded: vi.fn().mockResolvedValue(null),
      isAuthenticated: vi.fn().mockReturnValue(false),
      isTeacher: vi.fn().mockReturnValue(false),
      isStudent: vi.fn().mockReturnValue(false),
    };
    classContext = {
      readPersistedClassCode: vi.fn().mockReturnValue(null),
      isConfirmedForClass: vi.fn().mockReturnValue(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: auth },
        { provide: ClassContextService, useValue: classContext },
      ],
    });

    router = TestBed.inject(Router);
  });

  const loginState = { url: '/student/login' } as RouterStateSnapshot;

  it('redirects teachers to access denied', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.isTeacher.mockReturnValue(true);

    const route = {
      queryParamMap: { get: () => 'ENG7A' },
    } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => studentLoginGuard(route, loginState));

    expect(router.serializeUrl(result as UrlTree)).toBe('/access-denied');
  });

  it('accepts sessionStorage class code when query param is missing', async () => {
    classContext.readPersistedClassCode.mockReturnValue('ENG7A');
    classContext.isConfirmedForClass.mockReturnValue(true);

    const route = {
      queryParamMap: { get: () => null },
    } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => studentLoginGuard(route, loginState));

    expect(result).toBe(true);
    expect(classContext.isConfirmedForClass).toHaveBeenCalledWith('ENG7A');
  });

  it('redirects unconfirmed deep links back to class entry', async () => {
    const route = {
      queryParamMap: { get: () => 'ENG7A' },
    } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => studentLoginGuard(route, loginState));

    expect(router.serializeUrl(result as UrlTree)).toBe('/class?classCode=ENG7A');
  });
});
