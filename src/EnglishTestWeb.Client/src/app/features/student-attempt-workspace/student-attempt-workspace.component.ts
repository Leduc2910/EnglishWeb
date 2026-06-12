import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime } from 'rxjs';
import { SubmissionsApiService } from '../../core/submissions/submissions-api.service';
import {
  SUBMISSION_ERROR_MESSAGES,
  SUBMISSION_MODE_LABELS,
  SubmissionWorkspace,
} from '../../core/submissions/submissions.models';

type ViewState = 'loading' | 'loaded' | 'error';
type AutosaveStatus = 'idle' | 'saving' | 'saved' | 'error';

@Component({
  selector: 'app-student-attempt-workspace',
  templateUrl: './student-attempt-workspace.component.html',
  styleUrl: './student-attempt-workspace.component.css',
})
export class StudentAttemptWorkspaceComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly submissionsApi = inject(SubmissionsApiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);

  private submissionId: string | null = null;
  private readonly autosaveTrigger$ = new Subject<void>();

  protected readonly viewState = signal<ViewState>('loading');
  protected readonly workspace = signal<SubmissionWorkspace | null>(null);
  protected readonly errorCode = signal<string | null>(null);
  protected readonly answerInputs = signal<Record<number, string>>({});
  protected readonly autosaveStatus = signal<AutosaveStatus>('idle');

  protected readonly pdfUrl = computed<SafeResourceUrl | null>(() => {
    const ws = this.workspace();
    if (!ws || !this.submissionId) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      this.submissionsApi.getMaterialContentUrl(this.submissionId, ws.pdfMaterialId),
    );
  });

  protected readonly audioUrl = computed<SafeResourceUrl | null>(() => {
    const ws = this.workspace();
    if (!ws || ws.skill !== 'listening' || !ws.audioMaterialId || !this.submissionId) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      this.submissionsApi.getMaterialContentUrl(this.submissionId, ws.audioMaterialId),
    );
  });

  protected readonly answeredCount = computed(() =>
    Object.values(this.answerInputs()).filter((v) => v !== '').length,
  );

  protected readonly answerRange = computed<number[]>(() => {
    const ws = this.workspace();
    if (!ws) return [];
    return Array.from({ length: ws.questionCount }, (_, i) => i + 1);
  });

  protected readonly errorMessage = computed(() => {
    const code = this.errorCode();
    if (!code) return 'Không thể tải bài làm. Vui lòng thử lại.';
    return SUBMISSION_ERROR_MESSAGES[code] ?? 'Không thể tải bài làm. Vui lòng thử lại.';
  });

  protected readonly modeLabels = SUBMISSION_MODE_LABELS;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('submissionId');
    if (!id) {
      void this.router.navigate(['/student/tests']);
      return;
    }
    this.submissionId = id;

    this.autosaveTrigger$
      .pipe(debounceTime(800), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.performAutosave());

    void this.loadWorkspace(id);
  }

  protected onAnswerChange(questionNumber: number, value: string): void {
    this.answerInputs.update((current) => ({ ...current, [questionNumber]: value }));
    this.autosaveTrigger$.next();
  }

  protected jumpToFirstUnanswered(): void {
    const ws = this.workspace();
    if (!ws) return;
    const inputs = this.answerInputs();
    for (let qn = 1; qn <= ws.questionCount; qn++) {
      if (!inputs[qn]) {
        const el = document.getElementById(`answer-${qn}`);
        el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el?.focus();
        return;
      }
    }
  }

  protected onSubmit(): void {
    // Placeholder — story 4.4 implements submission logic
  }

  protected backToTests(): void {
    void this.router.navigate(['/student/tests']);
  }

  protected retryLoad(): void {
    if (!this.submissionId) return;
    void this.loadWorkspace(this.submissionId);
  }

  private async loadWorkspace(id: string): Promise<void> {
    this.viewState.set('loading');
    this.workspace.set(null);
    this.errorCode.set(null);

    try {
      const ws = await this.submissionsApi.getWorkspace(id);
      this.workspace.set(ws);

      const initialAnswers: Record<number, string> = {};
      for (const row of ws.answerRows) {
        if (row.answer !== null) {
          initialAnswers[row.questionNumber] = row.answer;
        }
      }
      this.answerInputs.set(initialAnswers);
      this.viewState.set('loaded');
    } catch (err: unknown) {
      this.errorCode.set(this.extractErrorCode(err));
      this.viewState.set('error');
    }
  }

  private async performAutosave(): Promise<void> {
    const id = this.submissionId;
    const ws = this.workspace();
    if (!id || !ws || ws.status === 'submitted') return;

    this.autosaveStatus.set('saving');

    const rows = Object.entries(this.answerInputs()).map(([qn, ans]) => ({
      questionNumber: Number(qn),
      answer: ans || null,
    }));

    try {
      await this.submissionsApi.autosaveAnswers(id, rows);
      this.autosaveStatus.set('saved');
    } catch {
      this.autosaveStatus.set('error');
    }
  }

  private extractErrorCode(err: unknown): string | null {
    if (err && typeof err === 'object' && 'error' in err) {
      const body = (err as { error: unknown }).error;
      if (body && typeof body === 'object' && 'extensions' in body) {
        const ext = (body as { extensions: unknown }).extensions;
        if (ext && typeof ext === 'object' && 'code' in ext) {
          return String((ext as { code: unknown }).code);
        }
      }
    }
    return null;
  }
}
