import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ClassContextService } from '../../core/classes/class-context.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { STUDENT_LOGIN_ERROR_MESSAGES } from '../../core/classes/classes.models';
import { normalizeClassCode } from '../../core/classes/class-code';

@Component({
  selector: 'app-student-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './student-login.component.html',
  styleUrl: './student-login.component.css',
})
export class StudentLoginComponent implements OnInit {
  private readonly auth = inject(AuthSessionService);
  protected readonly classContext = inject(ClassContextService);
  private readonly classesApi = inject(ClassesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private contextHydrationGeneration = 0;

  protected readonly showPassword = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly isLoadingContext = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly errorCode = signal<string | null>(null);
  protected readonly classCode = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    identifier: ['', Validators.required],
    password: ['', Validators.required],
  });

  async ngOnInit(): Promise<void> {
    const hydrationGeneration = this.contextHydrationGeneration;
    const queryCode = this.route.snapshot.queryParamMap.get('classCode');
    const persisted = this.classContext.readPersistedClassCode();
    const code = queryCode ?? persisted ?? this.classContext.confirmedClass()?.classCode ?? null;

    if (!code || !this.classContext.isConfirmedForClass(code)) {
      await this.router.navigate(['/class'], {
        queryParams: code ? { classCode: code } : {},
      });
      return;
    }

    const normalized = normalizeClassCode(code) ?? code.trim().toUpperCase();
    this.classCode.set(normalized);

    if (!this.classContext.confirmedClass()) {
      try {
        const preview = await this.classesApi.lookupByCode(normalized);
        if (hydrationGeneration !== this.contextHydrationGeneration) {
          return;
        }

        this.classContext.setConfirmedClass(preview);
      } catch {
        if (hydrationGeneration !== this.contextHydrationGeneration) {
          return;
        }

        if (this.auth.isAuthenticated() || this.classContext.activeClass()) {
          this.isLoadingContext.set(false);
          return;
        }

        this.classContext.clearClassContext();
        await this.router.navigate(['/class']);
        return;
      }
    }

    if (hydrationGeneration !== this.contextHydrationGeneration) {
      return;
    }

    this.isLoadingContext.set(false);
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update((value) => !value);
  }

  protected async changeClass(): Promise<void> {
    this.contextHydrationGeneration++;
    this.classContext.clearClassContext();
    await this.router.navigate(['/class']);
  }

  protected async onSubmit(): Promise<void> {
    if (this.isLoadingContext()) {
      return;
    }

    this.errorMessage.set(null);
    this.errorCode.set(null);

    if (this.form.invalid) {
      this.applyClientValidationErrors();
      return;
    }

    const code = this.classCode();
    if (!code) {
      await this.router.navigate(['/class']);
      return;
    }

    const normalized = normalizeClassCode(code);
    if (!normalized) {
      await this.router.navigate(['/class']);
      return;
    }

    this.isSubmitting.set(true);
    try {
      const value = this.form.getRawValue();
      await this.auth.loginStudent({
        identifier: value.identifier.trim(),
        password: value.password,
        classCode: normalized,
        rememberMe: false,
      });
      this.contextHydrationGeneration++;
      await this.router.navigate(['/student/tests']);
    } catch (error) {
      this.errorCode.set('ERR_STUDENT_LOGIN_INVALID');
      this.errorMessage.set(this.auth.mapStudentApiError(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private applyClientValidationErrors(): void {
    if (this.form.controls.identifier.hasError('required')) {
      this.errorCode.set('ERR_STUDENT_IDENTIFIER_REQUIRED');
      this.errorMessage.set(STUDENT_LOGIN_ERROR_MESSAGES['ERR_STUDENT_IDENTIFIER_REQUIRED']);
      return;
    }

    if (this.form.controls.password.hasError('required')) {
      this.errorCode.set('ERR_STUDENT_PASSWORD_REQUIRED');
      this.errorMessage.set(STUDENT_LOGIN_ERROR_MESSAGES['ERR_STUDENT_PASSWORD_REQUIRED']);
    }
  }
}
