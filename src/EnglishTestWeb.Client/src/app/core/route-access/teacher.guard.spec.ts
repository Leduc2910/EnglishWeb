import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { guestGuard, teacherGuard } from './teacher.guard';
import { AuthSessionService } from '../auth/auth-session.service';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

describe('teacherGuard', () => {
  let router: Router;
  let auth: {
    ensureSessionLoaded: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
    isTeacher: ReturnType<typeof vi.fn>;
  };

  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/teacher/dashboard' } as RouterStateSnapshot;

  beforeEach(() => {
    auth = {
      ensureSessionLoaded: vi.fn().mockResolvedValue(null),
      isAuthenticated: vi.fn().mockReturnValue(false),
      isTeacher: vi.fn().mockReturnValue(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: auth },
      ],
    });

    router = TestBed.inject(Router);
  });

  it('redirects unauthenticated users to login with returnUrl', async () => {
    const result = await TestBed.runInInjectionContext(() => teacherGuard(route, state));

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe(
      '/login?returnUrl=%2Fteacher%2Fdashboard',
    );
  });

  it('redirects authenticated non-teachers to access-denied', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.isTeacher.mockReturnValue(false);

    const result = await TestBed.runInInjectionContext(() => teacherGuard(route, state));

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/access-denied');
  });
});

describe('guestGuard', () => {
  let router: Router;
  let auth: {
    ensureSessionLoaded: ReturnType<typeof vi.fn>;
    isTeacher: ReturnType<typeof vi.fn>;
  };

  const state = { url: '/login' } as RouterStateSnapshot;

  beforeEach(() => {
    auth = {
      ensureSessionLoaded: vi.fn().mockResolvedValue(null),
      isTeacher: vi.fn().mockReturnValue(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: auth },
      ],
    });

    router = TestBed.inject(Router);
  });

  it('redirects authenticated teachers to returnUrl when provided', async () => {
    auth.isTeacher.mockReturnValue(true);
    const route = {
      queryParamMap: {
        get: (key: string) => (key === 'returnUrl' ? '/teacher/library' : null),
      },
    } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => guestGuard(route, state));

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/teacher/library');
  });

  it('redirects authenticated teachers to dashboard when returnUrl is absent', async () => {
    auth.isTeacher.mockReturnValue(true);
    const route = {
      queryParamMap: {
        get: (_key: string) => null,
      },
    } as unknown as ActivatedRouteSnapshot;

    const result = await TestBed.runInInjectionContext(() => guestGuard(route, state));

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/teacher/dashboard');
  });
});
