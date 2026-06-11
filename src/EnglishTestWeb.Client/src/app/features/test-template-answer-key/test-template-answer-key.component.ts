import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import {
  mapAnswerKeyApiError,
  mapTemplateApiError,
  ScoringMode,
  TEMPLATE_ERROR_MESSAGES,
} from '../../core/test-templates/test-templates.models';

interface AnswerRowState {
  questionNumber: number;
  correctAnswer: string;
  score: number | null;
}

interface ContinueValidationError {
  code: string;
  message: string;
}

const MIN_QUESTION_COUNT = 1;
const MAX_QUESTION_COUNT = 200;

@Component({
  selector: 'app-test-template-answer-key',
  imports: [RouterLink],
  templateUrl: './test-template-answer-key.component.html',
  styleUrl: './test-template-answer-key.component.css',
})
export class TestTemplateAnswerKeyComponent implements OnInit, OnDestroy {
  private readonly api = inject(TestTemplatesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private paramSubscription?: Subscription;
  private loadRequestId = 0;

  protected readonly templateId = signal<string | null>(null);
  protected readonly templateTitle = signal('');
  protected readonly templateSkill = signal('reading');
  protected readonly isLoading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly bannerError = signal<string | null>(null);
  protected readonly saveInFlight = signal(false);
  protected readonly saveSuccess = signal<string | null>(null);

  protected readonly questionCount = signal<number>(10);
  protected readonly questionCountInput = signal('10');
  protected readonly scoringMode = signal<ScoringMode>('equal');
  protected readonly totalScore = signal<number | null>(10);
  protected readonly rows = signal<AnswerRowState[]>([]);
  protected readonly continueErrors = signal<ContinueValidationError[]>([]);

  protected readonly isSpeaking = computed(() => this.templateSkill() === 'speaking');

  protected readonly questionCountValid = computed(() => {
    const count = this.questionCount();
    return Number.isInteger(count) && count >= MIN_QUESTION_COUNT && count <= MAX_QUESTION_COUNT;
  });

  protected readonly missingAnswerCount = computed(
    () => this.rows().filter((row) => !row.correctAnswer.trim()).length,
  );

  protected readonly scoreTotal = computed(() => {
    if (this.scoringMode() === 'equal') {
      return this.totalScore() ?? 0;
    }

    return this.rows().reduce((sum, row) => sum + (row.score ?? 0), 0);
  });

  protected readonly warningList = computed(() => {
    const warnings: string[] = [];

    if (!this.questionCountValid()) {
      warnings.push(TEMPLATE_ERROR_MESSAGES['ERR_QUESTION_COUNT_INVALID']);
    }

    const missing = this.missingAnswerCount();
    if (missing > 0) {
      warnings.push(`Còn ${missing} câu chưa có đáp án.`);
    }

    if (this.scoringMode() === 'equal') {
      const total = this.totalScore();
      if (!total || total <= 0) {
        warnings.push(TEMPLATE_ERROR_MESSAGES['ERR_TOTAL_SCORE_INVALID']);
      }
    } else if (this.rows().some((row) => !row.score || row.score <= 0)) {
      warnings.push(TEMPLATE_ERROR_MESSAGES['ERR_ROW_SCORE_INVALID']);
    }

    return warnings;
  });

  protected readonly canContinue = computed(
    () => this.warningList().length === 0 && !this.saveInFlight(),
  );

  ngOnInit(): void {
    this.paramSubscription = this.route.paramMap.subscribe((params) => {
      const templateId = params.get('templateId');
      if (!templateId) {
        void this.router.navigate(['/teacher/library']);
        return;
      }

      void this.loadPage(templateId);
    });
  }

  ngOnDestroy(): void {
    this.paramSubscription?.unsubscribe();
  }

  protected onQuestionCountChange(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    this.questionCountInput.set(raw);
    this.saveSuccess.set(null);
    this.continueErrors.set([]);

    const parsed = Number(raw);
    if (!raw.trim() || !Number.isFinite(parsed)) {
      this.questionCount.set(Number.NaN);
      return;
    }

    this.questionCount.set(parsed);
    if (!Number.isInteger(parsed) || parsed < MIN_QUESTION_COUNT || parsed > MAX_QUESTION_COUNT) {
      return;
    }

    this.applyQuestionCount(parsed);
  }

  protected onScoringModeChange(mode: ScoringMode): void {
    if (this.scoringMode() === mode) {
      return;
    }

    this.scoringMode.set(mode);
    this.saveSuccess.set(null);
    this.continueErrors.set([]);
  }

  protected onTotalScoreChange(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const parsed = Number(raw);
    this.totalScore.set(raw.trim() && Number.isFinite(parsed) ? parsed : null);
    this.saveSuccess.set(null);
    this.continueErrors.set([]);
  }

  protected onAnswerChange(questionNumber: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.rows.update((current) =>
      current.map((row) =>
        row.questionNumber === questionNumber ? { ...row, correctAnswer: value } : row,
      ),
    );
    this.saveSuccess.set(null);
    this.continueErrors.set([]);
  }

  protected onRowScoreChange(questionNumber: number, event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    const parsed = Number(raw);
    const score = raw.trim() && Number.isFinite(parsed) ? parsed : null;
    this.rows.update((current) =>
      current.map((row) => (row.questionNumber === questionNumber ? { ...row, score } : row)),
    );
    this.saveSuccess.set(null);
    this.continueErrors.set([]);
  }

  protected async onSaveDraft(): Promise<void> {
    const templateId = this.templateId();
    if (!templateId || this.saveInFlight()) {
      return;
    }

    if (!this.questionCountValid()) {
      this.bannerError.set(TEMPLATE_ERROR_MESSAGES['ERR_QUESTION_COUNT_INVALID']);
      return;
    }

    this.bannerError.set(null);
    this.saveSuccess.set(null);
    this.continueErrors.set([]);
    this.saveInFlight.set(true);

    try {
      const response = await this.api.upsertAnswerKey(templateId, {
        questionCount: this.questionCount(),
        scoringMode: this.scoringMode(),
        totalScore: this.scoringMode() === 'equal' ? this.totalScore() : null,
        rows: this.rows().map((row) => ({
          questionNumber: row.questionNumber,
          correctAnswer: row.correctAnswer.trim(),
          score: this.scoringMode() === 'per-question' ? row.score : null,
        })),
      });

      this.applyAnswerKey(response.questionCount, response.scoringMode, response.totalScore, response.rows);
      this.saveSuccess.set('Đã lưu nháp answer key.');
    } catch (error) {
      this.bannerError.set(mapAnswerKeyApiError(error));
    } finally {
      this.saveInFlight.set(false);
    }
  }

  protected async onContinue(): Promise<void> {
    if (this.saveInFlight()) {
      return;
    }

    this.saveSuccess.set(null);
    const errors = this.validateForContinue();
    this.continueErrors.set(errors);
    if (errors.length > 0) {
      return;
    }

    const templateId = this.templateId();
    if (!templateId) {
      return;
    }

    await this.onSaveDraft();
    if (this.bannerError()) {
      return;
    }

    await this.router.navigate(['/teacher/library', templateId, 'review']);
  }

  protected async onBack(): Promise<void> {
    const templateId = this.templateId();
    if (templateId) {
      await this.router.navigate(['/teacher/library', templateId, 'materials']);
      return;
    }

    await this.router.navigate(['/teacher/library']);
  }

  protected async goToReview(): Promise<void> {
    const templateId = this.templateId();
    if (templateId) {
      await this.router.navigate(['/teacher/library', templateId, 'review']);
    }
  }

  protected validateForContinue(): ContinueValidationError[] {
    const errors: ContinueValidationError[] = [];
    const count = this.questionCount();

    if (!Number.isInteger(count) || count < MIN_QUESTION_COUNT || count > MAX_QUESTION_COUNT) {
      errors.push({
        code: 'ERR_QUESTION_COUNT_INVALID',
        message: TEMPLATE_ERROR_MESSAGES['ERR_QUESTION_COUNT_INVALID'],
      });
      return errors;
    }

    for (const row of this.rows()) {
      if (!row.correctAnswer.trim()) {
        errors.push({
          code: 'ERR_ANSWER_MISSING',
          message: `Câu ${row.questionNumber} chưa có đáp án.`,
        });
      }
    }

    if (this.scoringMode() === 'equal') {
      const total = this.totalScore();
      if (!total || total <= 0) {
        errors.push({
          code: 'ERR_TOTAL_SCORE_INVALID',
          message: TEMPLATE_ERROR_MESSAGES['ERR_TOTAL_SCORE_INVALID'],
        });
      }
    } else {
      for (const row of this.rows()) {
        if (!row.score || row.score <= 0) {
          errors.push({
            code: 'ERR_ROW_SCORE_INVALID',
            message: `Điểm câu ${row.questionNumber} phải lớn hơn 0.`,
          });
        }
      }
    }

    return errors;
  }

  private applyQuestionCount(count: number): void {
    const current = this.rows();
    if (count < current.length && current.slice(count).some((row) => row.correctAnswer.trim())) {
      if (!confirm(`Giảm số câu xuống ${count} sẽ xóa đáp án của các câu phía sau. Tiếp tục?`)) {
        const previous = current.length;
        this.questionCount.set(previous);
        this.questionCountInput.set(String(previous));
        return;
      }
    }

    this.rows.update((existing) => {
      const next: AnswerRowState[] = [];
      for (let index = 0; index < count; index += 1) {
        next.push(existing[index] ?? { questionNumber: index + 1, correctAnswer: '', score: null });
      }

      return next.map((row, index) => ({ ...row, questionNumber: index + 1 }));
    });
  }

  private applyAnswerKey(
    questionCount: number,
    scoringMode: ScoringMode,
    totalScore: number | null,
    rows: { questionNumber: number; correctAnswer: string; score: number | null }[],
  ): void {
    this.questionCount.set(questionCount);
    this.questionCountInput.set(String(questionCount));
    this.scoringMode.set(scoringMode);
    this.totalScore.set(totalScore);

    const byNumber = new Map(rows.map((row) => [row.questionNumber, row]));
    const next: AnswerRowState[] = [];
    for (let index = 1; index <= questionCount; index += 1) {
      const existing = byNumber.get(index);
      next.push({
        questionNumber: index,
        correctAnswer: existing?.correctAnswer ?? '',
        score: existing?.score ?? null,
      });
    }

    this.rows.set(next);
  }

  private async loadPage(templateId: string): Promise<void> {
    const requestId = ++this.loadRequestId;
    this.isLoading.set(true);
    this.loadError.set(null);
    this.bannerError.set(null);

    try {
      const detail = await this.api.getTemplate(templateId);
      if (requestId !== this.loadRequestId) {
        return;
      }

      if (detail.status !== 'draft') {
        this.loadError.set(TEMPLATE_ERROR_MESSAGES['templates.notEditable']);
        this.templateId.set(null);
        return;
      }

      this.templateId.set(detail.templateId);
      this.templateTitle.set(detail.title);
      this.templateSkill.set(detail.skill);

      if (detail.skill === 'speaking') {
        return;
      }

      try {
        const answerKey = await this.api.getAnswerKey(templateId);
        if (requestId !== this.loadRequestId) {
          return;
        }

        this.applyAnswerKey(
          answerKey.questionCount,
          answerKey.scoringMode,
          answerKey.totalScore,
          answerKey.rows,
        );
      } catch {
        if (requestId !== this.loadRequestId) {
          return;
        }

        this.applyAnswerKey(10, 'equal', 10, []);
      }
    } catch (error) {
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.loadError.set(mapTemplateApiError(error));
      this.templateId.set(null);
    } finally {
      if (requestId === this.loadRequestId) {
        this.isLoading.set(false);
      }
    }
  }
}
