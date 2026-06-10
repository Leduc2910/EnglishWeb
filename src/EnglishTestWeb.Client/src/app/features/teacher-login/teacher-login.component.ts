import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { LOGIN_ERROR_MESSAGES } from '../../core/auth/auth.models';
import { sanitizeTeacherReturnUrl } from '../../core/route-access/return-url';

@Component({
  selector: 'app-teacher-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './teacher-login.component.html',
  styleUrl: './teacher-login.component.css',
})
export class TeacherLoginComponent {
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly showPassword = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly errorCode = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    identifier: ['', Validators.required],
    password: ['', Validators.required],
    rememberMe: [false],
  });

  protected togglePasswordVisibility(): void {
    this.showPassword.update((value) => !value);
  }

  protected async onSubmit(): Promise<void> {
    this.errorMessage.set(null);
    this.errorCode.set(null);

    if (this.form.invalid) {
      this.applyClientValidationErrors();
      return;
    }

    this.isSubmitting.set(true);

    try {
      const value = this.form.getRawValue();
      await this.auth.login({
        identifier: value.identifier.trim(),
        password: value.password,
        rememberMe: value.rememberMe,
      });

      const returnUrl = sanitizeTeacherReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
      await this.router.navigateByUrl(returnUrl ?? '/teacher/dashboard');
    } catch (error) {
      this.errorCode.set('ERR_LOGIN_INVALID');
      this.errorMessage.set(this.auth.mapApiError(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private applyClientValidationErrors(): void {
    const identifier = this.form.controls.identifier;
    const password = this.form.controls.password;

    if (identifier.hasError('required')) {
      this.errorCode.set('ERR_LOGIN_IDENTIFIER_REQUIRED');
      this.errorMessage.set(LOGIN_ERROR_MESSAGES['ERR_LOGIN_IDENTIFIER_REQUIRED']);
      return;
    }

    if (password.hasError('required')) {
      this.errorCode.set('ERR_LOGIN_PASSWORD_REQUIRED');
      this.errorMessage.set(LOGIN_ERROR_MESSAGES['ERR_LOGIN_PASSWORD_REQUIRED']);
    }
  }
}
