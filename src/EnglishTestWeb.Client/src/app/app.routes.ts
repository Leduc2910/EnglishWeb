import { Routes } from '@angular/router';
import { guestGuard, rootRedirectGuard, teacherGuard } from './core/route-access/teacher.guard';
import { studentGuard, studentLoginGuard } from './core/route-access/student.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [rootRedirectGuard],
    children: [],
  },
  {
    path: 'class',
    loadComponent: () =>
      import('./features/student-class-entry/student-class-entry.component').then(
        (module) => module.StudentClassEntryComponent,
      ),
  },
  {
    path: 'student/login',
    canActivate: [studentLoginGuard],
    loadComponent: () =>
      import('./features/student-login/student-login.component').then(
        (module) => module.StudentLoginComponent,
      ),
  },
  {
    path: 'student/tests',
    canActivate: [studentGuard],
    loadComponent: () =>
      import('./features/student-assigned-tests/student-assigned-tests.component').then(
        (module) => module.StudentAssignedTestsComponent,
      ),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/teacher-login/teacher-login.component').then(
        (module) => module.TeacherLoginComponent,
      ),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/forgot-password/forgot-password.component').then(
        (module) => module.ForgotPasswordComponent,
      ),
  },
  {
    path: 'access-denied',
    loadComponent: () =>
      import('./features/access-denied/access-denied.component').then(
        (module) => module.AccessDeniedComponent,
      ),
  },
  {
    path: 'teacher',
    canActivate: [teacherGuard],
    loadComponent: () =>
      import('./shared/layouts/teacher-shell/teacher-shell.component').then(
        (module) => module.TeacherShellComponent,
      ),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/teacher-dashboard/teacher-dashboard.component').then(
            (module) => module.TeacherDashboardComponent,
          ),
      },
      {
        path: 'library',
        loadComponent: () =>
          import('./features/teacher-placeholder/teacher-placeholder.component').then(
            (module) => module.TeacherPlaceholderComponent,
          ),
        data: {
          title: 'Thư viện đề',
          description: 'Epic 2 sẽ triển khai thư viện đề gốc.',
        },
      },
      {
        path: 'classes',
        loadComponent: () =>
          import('./features/teacher-classes/teacher-classes.component').then(
            (module) => module.TeacherClassesComponent,
          ),
      },
      {
        path: 'results',
        loadComponent: () =>
          import('./features/teacher-placeholder/teacher-placeholder.component').then(
            (module) => module.TeacherPlaceholderComponent,
          ),
        data: {
          title: 'Kết quả',
          description: 'Epic 6 sẽ triển khai Results & Grading.',
        },
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then((module) => module.NotFoundComponent),
  },
];
