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
    path: 'student/workspace/:submissionId',
    canActivate: [studentGuard],
    loadComponent: () =>
      import('./features/student-attempt-workspace/student-attempt-workspace.component').then(
        (module) => module.StudentAttemptWorkspaceComponent,
      ),
  },
  {
    path: 'student/speaking/:speakingSubmissionId',
    canActivate: [studentGuard],
    loadComponent: () =>
      import(
        './features/student-speaking-submission/student-speaking-submission.component'
      ).then((module) => module.StudentSpeakingSubmissionComponent),
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
          import('./features/test-template-library/test-template-library.component').then(
            (module) => module.TestTemplateLibraryComponent,
          ),
      },
      {
        path: 'library/new/setup',
        loadComponent: () =>
          import('./features/test-template-setup/test-template-setup.component').then(
            (module) => module.TestTemplateSetupComponent,
          ),
      },
      {
        path: 'library/:templateId/setup',
        loadComponent: () =>
          import('./features/test-template-setup/test-template-setup.component').then(
            (module) => module.TestTemplateSetupComponent,
          ),
      },
      {
        path: 'library/:templateId/materials',
        loadComponent: () =>
          import('./features/test-template-materials/test-template-materials.component').then(
            (module) => module.TestTemplateMaterialsComponent,
          ),
      },
      {
        path: 'library/:templateId/answer-key',
        loadComponent: () =>
          import('./features/test-template-answer-key/test-template-answer-key.component').then(
            (module) => module.TestTemplateAnswerKeyComponent,
          ),
      },
      {
        path: 'library/:templateId/review',
        loadComponent: () =>
          import('./features/test-template-review/test-template-review.component').then(
            (module) => module.TestTemplateReviewComponent,
          ),
      },
      {
        path: 'homework/new',
        loadComponent: () =>
          import('./features/homework-create/homework-create.component').then(
            (module) => module.HomeworkCreateComponent,
          ),
      },
      {
        path: 'live-exams/new',
        loadComponent: () =>
          import('./features/live-exam-create/live-exam-create.component').then(
            (module) => module.LiveExamCreateComponent,
          ),
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
