import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import {
  AnswerKeyVersionResponse,
  mapMarkReadyError,
  mapTemplateApiError,
  SKILL_LABELS,
  STATUS_LABELS,
  TestMaterialItem,
  TestTemplateDetail,
} from '../../core/test-templates/test-templates.models';

type ViewState = 'loading' | 'loaded' | 'savingReady' | 'success' | 'loadError';

@Component({
  selector: 'app-test-template-review',
  imports: [RouterLink],
  templateUrl: './test-template-review.component.html',
  styleUrl: './test-template-review.component.css',
})
export class TestTemplateReviewComponent implements OnInit, OnDestroy {
  private readonly api = inject(TestTemplatesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private paramSubscription?: Subscription;
  private loadRequestId = 0;

  protected readonly viewState = signal<ViewState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly markReadyError = signal<string | null>(null);

  protected readonly template = signal<TestTemplateDetail | null>(null);
  protected readonly materials = signal<TestMaterialItem[]>([]);
  protected readonly answerKey = signal<AnswerKeyVersionResponse | null>(null);

  protected readonly isSpeaking = computed(() => this.template()?.skill === 'speaking');

  protected readonly skillLabel = computed(
    () => SKILL_LABELS[this.template()?.skill ?? ''] ?? this.template()?.skill ?? '',
  );

  protected readonly statusLabel = computed(
    () => STATUS_LABELS[this.template()?.status ?? ''] ?? this.template()?.status ?? '',
  );

  protected readonly readinessChecks = computed(() => {
    const t = this.template();
    const mats = this.materials();
    const ak = this.answerKey();

    const isReadingOrListening = t?.skill === 'reading' || t?.skill === 'listening';
    const materialPassed = isReadingOrListening
      ? mats.some((m) => m.role === 'pdf')
      : mats.length > 0;

    const checks: { id: string; label: string; passed: boolean }[] = [
      {
        id: 'info',
        label: 'Thông tin đề',
        passed: !!t?.title && !!t?.skill,
      },
      {
        id: 'material',
        label: isReadingOrListening ? 'File PDF đề bài' : 'Tài liệu bắt buộc',
        passed: materialPassed,
      },
    ];

    if (t?.skill !== 'speaking') {
      const rows = ak?.rows ?? [];
      const allAnswered =
        !!ak && rows.length === ak.questionCount && rows.every((r) => r.correctAnswer.trim());
      const scoringValid =
        !!ak &&
        (ak.scoringMode === 'equal'
          ? (ak.totalScore ?? 0) > 0
          : rows.every((r) => (r.score ?? 0) > 0));

      checks.push({ id: 'answerKey', label: 'Answer key hoàn tất', passed: allAnswered });
      checks.push({ id: 'scoring', label: 'Cấu hình điểm hợp lệ', passed: scoringValid });
    }

    return checks;
  });

  protected readonly isLoaded = computed(() => this.viewState() === 'loaded');
  protected readonly isSuccess = computed(() => this.viewState() === 'success');
  protected readonly isSavingReady = computed(() => this.viewState() === 'savingReady');

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

  protected async onMarkReady(): Promise<void> {
    const templateId = this.template()?.templateId;
    if (!templateId || this.isSavingReady()) {
      return;
    }

    this.viewState.set('savingReady');
    this.markReadyError.set(null);

    try {
      const updated = await this.api.markReady(templateId);
      this.template.set(updated);
      this.viewState.set('success');
    } catch (error) {
      this.markReadyError.set(mapMarkReadyError(error));
      this.viewState.set('loaded');
    }
  }

  protected async onBack(): Promise<void> {
    const tmpl = this.template();
    if (tmpl) {
      const prevStep = tmpl.skill === 'speaking' ? 'materials' : 'answer-key';
      await this.router.navigate(['/teacher/library', tmpl.templateId, prevStep]);
      return;
    }

    await this.router.navigate(['/teacher/library']);
  }

  protected async onGoToLibrary(): Promise<void> {
    await this.router.navigate(['/teacher/library']);
  }

  private async loadPage(templateId: string): Promise<void> {
    const requestId = ++this.loadRequestId;
    this.viewState.set('loading');
    this.loadError.set(null);
    this.markReadyError.set(null);

    try {
      const detail = await this.api.getTemplate(templateId);
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.template.set(detail);

      const mats = await this.api.listMaterials(templateId);
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.materials.set(mats);

      if (detail.skill !== 'speaking') {
        try {
          const ak = await this.api.getAnswerKey(templateId);
          if (requestId !== this.loadRequestId) {
            return;
          }

          this.answerKey.set(ak);
        } catch {
          if (requestId !== this.loadRequestId) {
            return;
          }

          this.answerKey.set(null);
        }
      }

      this.viewState.set(detail.status === 'ready' ? 'success' : 'loaded');
    } catch (error) {
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.loadError.set(mapTemplateApiError(error));
      this.viewState.set('loadError');
    }
  }
}
