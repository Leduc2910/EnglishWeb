import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSessionService } from '../auth/auth-session.service';
import { ClassContextService } from '../classes/class-context.service';
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
  const classContext = inject(ClassContextService);
  const router = inject(Router);

  await auth.ensureSessionLoaded();

  if (auth.isTeacher()) {
    return router.createUrlTree(['/teacher/dashboard']);
  }

  if (auth.isStudent()) {
    if (classContext.activeClass() || classContext.readPersistedClassCode()) {
      return router.createUrlTree(['/student/tests']);
    }

    return router.createUrlTree(['/class']);
  }

  return router.createUrlTree(['/class']);
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
