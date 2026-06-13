import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import {
  RESULT_MODE_LABELS,
  RESULT_SKILL_LABELS,
  RESULT_STATUS_LABELS,
  ResultRowDto,
  ResultsFilter,
  ResultsPageDto,
  TeacherSubmissionDetailDto,
} from '../../core/results/results.models';
import { ResultsApiService } from '../../core/results/results-api.service';
import {
  GradeSpeakingRequest,
  SPEAKING_ERROR_MESSAGES,
  TeacherSpeakingSubmissionDto,
} from '../../core/speaking/speaking.models';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { ClassSummary } from '../../core/classes/classes.models';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { TestTemplateListItem } from '../../core/test-templates/test-templates.models';

type LoadState   = 'loading' | 'loaded' | 'error';
type DetailState = 'closed' | 'loading' | 'rl-loaded' | 'speaking-loaded' | 'error';
type GradeState  = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-teacher-results',
  templateUrl: './teacher-results.component.html',
  styleUrl: './teacher-results.component.css',
  imports: [FormsModule],
})
export class TeacherResultsComponent implements OnInit, OnDestroy {
  private readonly api           = inject(ResultsApiService);
  private readonly speakingApi   = inject(SpeakingApiService);
  private readonly classesApi    = inject(ClassesApiService);
  private readonly templatesApi  = inject(TestTemplatesApiService);
  private readonly sanitizer     = inject(DomSanitizer);

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private currentRequestId = 0;

  protected readonly Math = Math;

  // Filter signals
  protected readonly filterClass    = signal<string>('');
  protected readonly filterMode     = signal<string>('');
  protected readonly filterTemplate = signal<string>('');
  protected readonly filterStudent  = signal<string>('');
  protected readonly filterSkill    = signal<string>('');
  protected readonly filterStatus   = signal<string>('');

  // Filter dropdown data
  protected readonly availableClasses   = signal<ClassSummary[]>([]);
  protected readonly availableTemplates = signal<TestTemplateListItem[]>([]);

  // Pagination
  protected readonly currentPage = signal<number>(1);
  protected readonly pageSize    = signal<number>(20);

  // Results list
  protected readonly loadState    = signal<LoadState>('loading');
  protected readonly results      = signal<ResultsPageDto | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  // Selection
  protected readonly selectedRowId = signal<string | null>(null);

  // Detail panel
  protected readonly detailState        = signal<DetailState>('closed');
  protected readonly rlDetail           = signal<TeacherSubmissionDetailDto | null>(null);
  protected readonly speakingDetail     = signal<TeacherSpeakingSubmissionDto | null>(null);
  protected readonly detailErrorMessage = signal<string | null>(null);

  // Speaking grading
  protected readonly scoreInput        = signal<string>('');
  protected readonly feedbackInput     = signal<string>('');
  protected readonly gradeState        = signal<GradeState>('idle');
  protected readonly gradeErrorMessage = signal<string | null>(null);

  // Label maps
  protected readonly modeLabelMap   = RESULT_MODE_LABELS;
  protected readonly skillLabelMap  = RESULT_SKILL_LABELS;
  protected readonly statusLabelMap = RESULT_STATUS_LABELS;

  protected readonly audioUrl = computed((): SafeResourceUrl | null => {
    const rowId = this.selectedRowId();
    if (!rowId) return null;
    const row = this.results()?.items.find(r => r.id === rowId);
    if (!row || row.type !== 'speaking') return null;
    const url = this.speakingApi.getTeacherSubmissionFileUrl(rowId);
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  protected readonly nextPendingSpeakingRow = computed((): ResultRowDto | null => {
    const currentId = this.selectedRowId();
    const items = this.results()?.items ?? [];
    const pending = items.filter(r => r.type === 'speaking' && r.status === 'submitted');
    if (pending.length === 0) return null;
    const idx = pending.findIndex(r => r.id === currentId);
    if (idx === -1) return pending[0] ?? null;
    return pending[idx + 1] ?? null;
  });

  ngOnInit(): void {
    void this.loadFilterDropdowns();
    void this.loadResults();
  }

  ngOnDestroy(): void {
    if (this.debounceTimer !== null) {
      clearTimeout(this.debounceTimer);
    }
  }

  protected onFilterChange(): void {
    this.currentPage.set(1);
    this.scheduleDebouncedLoad();
  }

  protected onClearFilters(): void {
    if (this.debounceTimer !== null) {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = null;
    }
    this.filterClass.set('');
    this.filterMode.set('');
    this.filterTemplate.set('');
    this.filterStudent.set('');
    this.filterSkill.set('');
    this.filterStatus.set('');
    this.currentPage.set(1);
    void this.loadResults();
  }

  protected onSelectRow(row: ResultRowDto): void {
    if (this.selectedRowId() === row.id && this.detailState() !== 'error') return;
    this.selectedRowId.set(row.id);
    this.gradeState.set('idle');
    this.gradeErrorMessage.set(null);
    void this.loadDetail(row);
  }

  protected onCloseDetail(): void {
    this.selectedRowId.set(null);
    this.detailState.set('closed');
    this.rlDetail.set(null);
    this.speakingDetail.set(null);
  }

  protected onPageChange(newPage: number): void {
    this.currentPage.set(newPage);
    void this.loadResults();
  }

  protected async onGradeSubmit(): Promise<void> {
    if (this.gradeState() === 'submitting') return;
    const scoreStr = this.scoreInput().trim();
    const score = scoreStr === '' ? null : Number(scoreStr);
    if (score === null || !Number.isInteger(score) || score < 0 || score > 10) {
      this.gradeErrorMessage.set('Điểm số phải là số nguyên từ 0 đến 10.');
      return;
    }
    const rowId = this.selectedRowId();
    if (!rowId) return;

    this.gradeState.set('submitting');
    this.gradeErrorMessage.set(null);

    const request: GradeSpeakingRequest = {
      score,
      feedback: this.feedbackInput().trim() || null,
    };
    try {
      const updated = await this.speakingApi.grade(rowId, request);
      this.speakingDetail.set(updated);
      this.gradeState.set('success');
      this.updateResultRow(rowId, 'graded', score);
    } catch (err: unknown) {
      this.gradeState.set('error');
      const code = this.extractErrorCode(err);
      this.gradeErrorMessage.set(
        SPEAKING_ERROR_MESSAGES[code ?? ''] ?? 'Chấm điểm thất bại. Vui lòng thử lại.',
      );
    }
  }

  protected onNextPending(): void {
    const next = this.nextPendingSpeakingRow();
    if (next) {
      this.selectedRowId.set(null); // reset so onSelectRow doesn't short-circuit
      this.onSelectRow(next);
    }
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString('vi-VN');
  }

  protected formatScore(score: number | null, type: string): string {
    if (score === null) return '—';
    if (type === 'speaking') return String(Math.round(score));
    return score.toFixed(1);
  }

  protected formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private scheduleDebouncedLoad(): void {
    if (this.debounceTimer !== null) {
      clearTimeout(this.debounceTimer);
    }
    this.debounceTimer = setTimeout(() => {
      this.debounceTimer = null;
      void this.loadResults();
    }, 300);
  }

  private async loadFilterDropdowns(): Promise<void> {
    const [classes, templates] = await Promise.all([
      this.classesApi.getTeacherClasses().catch(() => [] as ClassSummary[]),
      this.templatesApi.listTemplates({ skill: '', status: '', q: '' }).catch(() => [] as TestTemplateListItem[]),
    ]);
    this.availableClasses.set(classes);
    this.availableTemplates.set(templates);
  }

  private async loadResults(): Promise<void> {
    const requestId = ++this.currentRequestId;
    this.loadState.set('loading');
    this.errorMessage.set(null);
    this.selectedRowId.set(null);
    this.detailState.set('closed');

    const filter: ResultsFilter = {
      classId:    this.filterClass()    || undefined,
      mode:       (this.filterMode()    || undefined) as ResultsFilter['mode'],
      templateId: this.filterTemplate() || undefined,
      q:          this.filterStudent()  || undefined,
      skill:      (this.filterSkill()   || undefined) as ResultsFilter['skill'],
      status:     this.filterStatus()   || undefined,
      page:       this.currentPage(),
      pageSize:   this.pageSize(),
      sort:       'submittedAt',
      direction:  'desc',
    };

    try {
      const data = await this.api.getResults(filter);
      if (requestId !== this.currentRequestId) return;
      this.results.set(data);
      this.loadState.set('loaded');
    } catch {
      if (requestId !== this.currentRequestId) return;
      this.loadState.set('error');
      this.errorMessage.set('Không thể tải kết quả. Vui lòng thử lại.');
    }
  }

  private async loadDetail(row: ResultRowDto): Promise<void> {
    this.detailState.set('loading');
    this.detailErrorMessage.set(null);
    try {
      if (row.type === 'speaking') {
        const dto = await this.speakingApi.getForTeacher(row.id);
        this.speakingDetail.set(dto);
        this.scoreInput.set(dto.score !== null ? String(dto.score) : '');
        this.feedbackInput.set(dto.feedback ?? '');
        this.detailState.set('speaking-loaded');
      } else {
        const dto = await this.api.getSubmissionDetail(row.id);
        this.rlDetail.set(dto);
        this.detailState.set('rl-loaded');
      }
    } catch {
      this.detailState.set('error');
      this.detailErrorMessage.set('Không thể tải chi tiết. Vui lòng thử lại.');
    }
  }

  private updateResultRow(rowId: string, newStatus: string, score: number): void {
    const current = this.results();
    if (!current) return;
    const updatedItems = current.items.map(r =>
      r.id === rowId ? { ...r, status: newStatus, score } : r,
    );
    this.results.set({ ...current, items: updatedItems });
  }

  private extractErrorCode(err: unknown): string | null {
    if (err && typeof err === 'object' && 'error' in err) {
      const body = (err as { error: unknown }).error;
      if (body && typeof body === 'object' && 'extensions' in body) {
        const ext = (body as { extensions: unknown }).extensions;
        if (ext && typeof ext === 'object' && 'code' in ext)
          return String((ext as { code: unknown }).code);
      }
    }
    return null;
  }
}
