import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSessionService } from '../auth/auth-session.service';
import { ClassContextService } from '../classes/class-context.service';

export const studentGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthSessionService);
  const classContext = inject(ClassContextService);
  const router = inject(Router);

  await auth.ensureSessionLoaded();

  if (auth.isTeacher()) {
    return router.createUrlTree(['/access-denied']);
  }

  if (!auth.isAuthenticated()) {
    const classCode = classContext.readPersistedClassCode();
    if (classCode) {
      return router.createUrlTree(['/student/login'], {
        queryParams: { classCode },
        queryParamsHandling: 'merge',
      });
    }

    return router.createUrlTree(['/class'], {
      queryParams: { returnUrl: state.url },
    });
  }

  if (!auth.isStudent()) {
    return router.createUrlTree(['/access-denied']);
  }

  if (!classContext.activeClass()) {
    return router.createUrlTree(['/class']);
  }

  return true;
};

export const studentLoginGuard: CanActivateFn = async (route) => {
  const auth = inject(AuthSessionService);
  const classContext = inject(ClassContextService);
  const router = inject(Router);

  await auth.ensureSessionLoaded();

  if (auth.isAuthenticated() && auth.isStudent()) {
    return router.createUrlTree(['/student/tests']);
  }

  if (auth.isAuthenticated() && auth.isTeacher()) {
    return router.createUrlTree(['/access-denied']);
  }

  const classCode =
    route.queryParamMap.get('classCode') ?? classContext.readPersistedClassCode();
  if (!classCode) {
    return router.createUrlTree(['/class']);
  }

  if (!classContext.isConfirmedForClass(classCode)) {
    return router.createUrlTree(['/class'], {
      queryParams: { classCode },
    });
  }

  return true;
};
