import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { ClassContextService } from '../../core/classes/class-context.service';
import { CLASS_ERROR_MESSAGES, ClassLookupPreview } from '../../core/classes/classes.models';
import { formatClassCodeInput, normalizeClassCode } from '../../core/classes/class-code';
import { HttpErrorResponse } from '@angular/common/http';
import { readProblemCode } from '../../core/http/problem-details';
import { API_CLASS_ERROR_MESSAGES } from '../../core/classes/classes.models';

@Component({
  selector: 'app-student-class-entry',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './student-class-entry.component.html',
  styleUrl: './student-class-entry.component.css',
})
export class StudentClassEntryComponent implements OnInit {
  private readonly classesApi = inject(ClassesApiService);
  private readonly classContext = inject(ClassContextService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly errorCode = signal<string | null>(null);
  protected readonly lookupResult = signal<ClassLookupPreview | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    classCode: ['', Validators.required],
  });

  ngOnInit(): void {
    const prefill = this.route.snapshot.queryParamMap.get('classCode');
    if (prefill) {
      this.form.controls.classCode.setValue(formatClassCodeInput(prefill));
    }
  }

  protected onCodeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = formatClassCodeInput(input.value);
    this.form.controls.classCode.setValue(formatted, { emitEvent: false });
    input.value = formatted;
  }

  protected async onLookup(): Promise<void> {
    this.errorMessage.set(null);
    this.errorCode.set(null);
    this.lookupResult.set(null);

    const raw = this.form.controls.classCode.value;
    if (!raw.trim()) {
      this.errorCode.set('ERR_CLASS_CODE_REQUIRED');
      this.errorMessage.set(CLASS_ERROR_MESSAGES['ERR_CLASS_CODE_REQUIRED']);
      return;
    }

    const normalized = normalizeClassCode(raw);
    if (!normalized) {
      this.errorCode.set('ERR_CLASS_CODE_FORMAT');
      this.errorMessage.set(CLASS_ERROR_MESSAGES['ERR_CLASS_CODE_FORMAT']);
      return;
    }

    this.isSubmitting.set(true);
    try {
      const preview = await this.classesApi.lookupByCode(normalized);
      this.lookupResult.set(preview);
    } catch (error) {
      this.applyLookupError(error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  protected changeCode(): void {
    this.lookupResult.set(null);
    this.errorMessage.set(null);
    this.errorCode.set(null);
  }

  protected async confirmClass(): Promise<void> {
    const preview = this.lookupResult();
    if (!preview) {
      return;
    }

    this.classContext.setConfirmedClass(preview);
    await this.router.navigate(['/student/login'], {
      queryParams: { classCode: preview.classCode },
    });
  }

  private applyLookupError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
      const code = readProblemCode(error.error as { code?: string; extensions?: { code?: string } });
      if (code === 'classes.codeInactive') {
        this.errorCode.set('ERR_CLASS_CODE_EXPIRED');
        this.errorMessage.set(CLASS_ERROR_MESSAGES['ERR_CLASS_CODE_EXPIRED']);
        return;
      }

      if (code && API_CLASS_ERROR_MESSAGES[code]) {
        this.errorCode.set(code === 'classes.codeNotFound' ? 'ERR_CLASS_CODE_INVALID' : 'ERR_CLASS_CODE_EXPIRED');
        this.errorMessage.set(API_CLASS_ERROR_MESSAGES[code]);
        return;
      }
    }

    this.errorCode.set('ERR_CLASS_CODE_INVALID');
    this.errorMessage.set(CLASS_ERROR_MESSAGES['ERR_CLASS_CODE_INVALID']);
  }
}
