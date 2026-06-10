import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSessionService } from '../auth/auth-session.service';
import { sanitizeTeacherReturnUrl } from './return-url';

export const teacherGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthSessionService);
  const router = inject(Router);

  await auth.ensureSessionLoaded();

  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }

  if (!auth.isTeacher()) {
    return router.createUrlTree(['/access-denied']);
  }

  return true;
};

export const rootRedirectGuard: CanActivateFn = async () => {
  const auth = inject(AuthSessionService);
  const router = inject(Router);

  await auth.ensureSessionLoaded();

  if (auth.isTeacher()) {
    return router.createUrlTree(['/teacher/dashboard']);
  }

  return router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = async (route) => {
  const auth = inject(AuthSessionService);
  const router = inject(Router);

  await auth.ensureSessionLoaded();

  if (auth.isTeacher()) {
    const returnUrl = sanitizeTeacherReturnUrl(route.queryParamMap.get('returnUrl'));
    return router.createUrlTree([returnUrl ?? '/teacher/dashboard']);
  }

  return true;
};
