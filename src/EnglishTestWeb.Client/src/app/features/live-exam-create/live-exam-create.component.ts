import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { ClassSummary } from '../../core/classes/classes.models';
import { LiveExamApiService } from '../../core/live-exam/live-exam-api.service';
import { LiveExamSession, mapLiveExamError } from '../../core/live-exam/live-exam.models';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { SKILL_LABELS, TestTemplateDetail } from '../../core/test-templates/test-templates.models';

type ViewState = 'loading' | 'loaded' | 'saving' | 'created' | 'loadError';
type SessionAction = 'idle' | 'opening' | 'closing';

@Component({
  selector: 'app-live-exam-create',
  imports: [RouterLink],
  templateUrl: './live-exam-create.component.html',
  styleUrl: './live-exam-create.component.css',
})
export class LiveExamCreateComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly templatesApi = inject(TestTemplatesApiService);
  private readonly classesApi = inject(ClassesApiService);
  private readonly liveExamApi = inject(LiveExamApiService);

  private paramSubscription?: Subscription;
  private loadRequestId = 0;

  protected readonly viewState = signal<ViewState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly apiError = signal<string | null>(null);

  protected readonly templateId = signal<string | null>(null);
  protected readonly template = signal<TestTemplateDetail | null>(null);
  protected readonly classes = signal<ClassSummary[]>([]);
  protected readonly session = signal<LiveExamSession | null>(null);
  protected readonly sessionAction = signal<SessionAction>('idle');

  protected readonly selectedClassId = signal<string>('');
  protected readonly scheduledStartAt = signal<string>('');
  protected readonly scheduledEndAt = signal<string>('');

  protected readonly skillLabel = computed(
    () => SKILL_LABELS[this.template()?.skill ?? ''] ?? this.template()?.skill ?? '',
  );
  protected readonly isLoading = computed(() => this.viewState() === 'loading');
  protected readonly isSaving = computed(() => this.viewState() === 'saving');
  protected readonly isCreated = computed(() => this.viewState() === 'created');
  protected readonly isLoadError = computed(() => this.viewState() === 'loadError');
  protected readonly activeClasses = computed(() =>
    this.classes().filter((c) => c.status === 'active'),
  );
  protected readonly isFormValid = computed(
    () => !!this.selectedClassId() && this.template()?.status === 'ready',
  );
  protected readonly canOpen = computed(() => this.session()?.status === 'scheduled');
  protected readonly canClose = computed(() => this.session()?.status === 'open');
  protected readonly isOpening = computed(() => this.sessionAction() === 'opening');
  protected readonly isClosing = computed(() => this.sessionAction() === 'closing');

  ngOnInit(): void {
    this.paramSubscription = this.route.queryParamMap.subscribe((params) => {
      const id = params.get('templateId');
      if (!id) {
        void this.router.navigate(['/teacher/library']);
        return;
      }
      this.templateId.set(id);
      void this.loadPage(id);
    });
  }

  ngOnDestroy(): void {
    this.paramSubscription?.unsubscribe();
  }

  protected async onCreate(): Promise<void> {
    const tid = this.templateId();
    const classId = this.selectedClassId();
    if (!tid || !classId || this.isSaving()) {
      return;
    }

    this.viewState.set('saving');
    this.apiError.set(null);

    const startRaw = this.scheduledStartAt();
    const endRaw = this.scheduledEndAt();

    try {
      const result = await this.liveExamApi.create({
        templateId: tid,
        classId,
        scheduledStartAt: startRaw ? new Date(startRaw).toISOString() : null,
        scheduledEndAt: endRaw ? new Date(endRaw).toISOString() : null,
      });
      this.session.set(result);
      this.viewState.set('created');
    } catch (error) {
      this.apiError.set(mapLiveExamError(error));
      this.viewState.set('loaded');
    }
  }

  protected async onOpen(): Promise<void> {
    const s = this.session();
    if (!s || this.sessionAction() !== 'idle') {
      return;
    }
    this.sessionAction.set('opening');
    this.apiError.set(null);
    try {
      const updated = await this.liveExamApi.open(s.id);
      this.session.set(updated);
    } catch (error) {
      this.apiError.set(mapLiveExamError(error));
    } finally {
      this.sessionAction.set('idle');
    }
  }

  protected async onClose(): Promise<void> {
    const s = this.session();
    if (!s || this.sessionAction() !== 'idle') {
      return;
    }
    this.sessionAction.set('closing');
    this.apiError.set(null);
    try {
      const updated = await this.liveExamApi.close(s.id);
      this.session.set(updated);
    } catch (error) {
      this.apiError.set(mapLiveExamError(error));
    } finally {
      this.sessionAction.set('idle');
    }
  }

  protected onCancel(): void {
    const tid = this.templateId();
    if (tid) {
      void this.router.navigate(['/teacher/library', tid, 'review']);
    } else {
      void this.router.navigate(['/teacher/library']);
    }
  }

  protected onClassChange(event: Event): void {
    this.selectedClassId.set((event.target as HTMLSelectElement).value);
  }

  protected onScheduledStartChange(event: Event): void {
    this.scheduledStartAt.set((event.target as HTMLInputElement).value);
  }

  protected onScheduledEndChange(event: Event): void {
    this.scheduledEndAt.set((event.target as HTMLInputElement).value);
  }

  protected formatDate(iso: string | null | undefined): string {
    if (!iso) return '';
    return new Date(iso).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' });
  }

  protected getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      scheduled: 'Đã lên lịch',
      open: 'Đang mở',
      closed: 'Đã đóng',
    };
    return labels[status] ?? status;
  }

  private async loadPage(tid: string): Promise<void> {
    const requestId = ++this.loadRequestId;
    this.viewState.set('loading');
    this.loadError.set(null);
    this.apiError.set(null);

    try {
      const [template, classes] = await Promise.all([
        this.templatesApi.getTemplate(tid),
        this.classesApi.getTeacherClasses(),
      ]);

      if (requestId !== this.loadRequestId) {
        return;
      }

      this.template.set(template);
      this.classes.set(classes);

      if (template.status !== 'ready') {
        this.loadError.set('Đề gốc chưa ở trạng thái Sẵn sàng. Không thể tạo phiên thi.');
        this.viewState.set('loadError');
        return;
      }

      this.viewState.set('loaded');
    } catch {
      if (requestId !== this.loadRequestId) {
        return;
      }
      this.loadError.set('Không thể tải thông tin đề gốc. Vui lòng thử lại.');
      this.viewState.set('loadError');
    }
  }
}
