import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { ClassSummary } from '../../core/classes/classes.models';
import { HomeworkApiService } from '../../core/homework/homework-api.service';
import { HomeworkAssignment, mapHomeworkCreateError } from '../../core/homework/homework.models';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { SKILL_LABELS, TestTemplateDetail } from '../../core/test-templates/test-templates.models';

type ViewState = 'loading' | 'loaded' | 'saving' | 'success' | 'loadError';

@Component({
  selector: 'app-homework-create',
  imports: [RouterLink],
  templateUrl: './homework-create.component.html',
  styleUrl: './homework-create.component.css',
})
export class HomeworkCreateComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly templatesApi = inject(TestTemplatesApiService);
  private readonly classesApi = inject(ClassesApiService);
  private readonly homeworkApi = inject(HomeworkApiService);

  private paramSubscription?: Subscription;
  private loadRequestId = 0;

  protected readonly viewState = signal<ViewState>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly apiError = signal<string | null>(null);

  protected readonly templateId = signal<string | null>(null);
  protected readonly template = signal<TestTemplateDetail | null>(null);
  protected readonly classes = signal<ClassSummary[]>([]);
  protected readonly assignment = signal<HomeworkAssignment | null>(null);

  protected readonly selectedClassId = signal<string>('');
  protected readonly deadlineAt = signal<string>('');
  protected readonly timeLimitMinutes = signal<string>('');

  protected readonly skillLabel = computed(
    () => SKILL_LABELS[this.template()?.skill ?? ''] ?? this.template()?.skill ?? '',
  );
  protected readonly isLoading = computed(() => this.viewState() === 'loading');
  protected readonly isSaving = computed(() => this.viewState() === 'saving');
  protected readonly isSuccess = computed(() => this.viewState() === 'success');
  protected readonly isLoadError = computed(() => this.viewState() === 'loadError');
  protected readonly activeClasses = computed(() =>
    this.classes().filter((c) => c.status === 'active'),
  );
  protected readonly isFormValid = computed(
    () =>
      !!this.selectedClassId() &&
      !!this.deadlineAt() &&
      this.template()?.status === 'ready',
  );

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

  protected async onSubmit(): Promise<void> {
    const tid = this.templateId();
    const classId = this.selectedClassId();
    const deadline = this.deadlineAt();

    if (!tid || !classId || !deadline || this.isSaving()) {
      return;
    }

    this.viewState.set('saving');
    this.apiError.set(null);

    const timeLimitRaw = parseInt(this.timeLimitMinutes(), 10);
    const timeLimitMinutes = isNaN(timeLimitRaw) || this.timeLimitMinutes() === '' ? null : timeLimitRaw;

    try {
      const result = await this.homeworkApi.create({
        templateId: tid,
        classId,
        deadlineAt: new Date(deadline).toISOString(),
        timeLimitMinutes,
      });
      this.assignment.set(result);
      this.viewState.set('success');
    } catch (error) {
      this.apiError.set(mapHomeworkCreateError(error));
      this.viewState.set('loaded');
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

  protected formatDeadline(iso?: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' });
  }

  protected onGoToLibrary(): void {
    void this.router.navigate(['/teacher/library']);
  }

  protected onClassChange(event: Event): void {
    this.selectedClassId.set((event.target as HTMLSelectElement).value);
  }

  protected onDeadlineChange(event: Event): void {
    this.deadlineAt.set((event.target as HTMLInputElement).value);
  }

  protected onTimeLimitChange(event: Event): void {
    this.timeLimitMinutes.set((event.target as HTMLInputElement).value);
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
        this.loadError.set('Đề gốc chưa ở trạng thái Sẵn sàng. Không thể giao homework.');
        this.viewState.set('loadError');
        return;
      }

      this.viewState.set('loaded');
    } catch (error) {
      if (requestId !== this.loadRequestId) {
        return;
      }
      this.loadError.set('Không thể tải thông tin đề gốc. Vui lòng thử lại.');
      this.viewState.set('loadError');
    }
  }
}
