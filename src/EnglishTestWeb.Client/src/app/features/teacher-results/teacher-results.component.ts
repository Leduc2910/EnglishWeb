import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  RESULT_MODE_LABELS,
  RESULT_SKILL_LABELS,
  RESULT_STATUS_LABELS,
  ResultRowDto,
  ResultsFilter,
  ResultsPageDto,
} from '../../core/results/results.models';
import { ResultsApiService } from '../../core/results/results-api.service';

type LoadState = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-teacher-results',
  templateUrl: './teacher-results.component.html',
  styleUrl: './teacher-results.component.css',
  imports: [FormsModule],
})
export class TeacherResultsComponent implements OnInit, OnDestroy {
  private readonly api = inject(ResultsApiService);
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private currentRequestId = 0;

  protected readonly Math = Math;

  protected readonly filterMode     = signal<string>('');
  protected readonly filterSkill    = signal<string>('');
  protected readonly filterStatus   = signal<string>('');
  protected readonly filterStudent  = signal<string>('');

  protected readonly currentPage    = signal<number>(1);
  protected readonly pageSize       = signal<number>(20);

  protected readonly loadState      = signal<LoadState>('loading');
  protected readonly results        = signal<ResultsPageDto | null>(null);
  protected readonly errorMessage   = signal<string | null>(null);
  protected readonly selectedRowId  = signal<string | null>(null);

  protected readonly modeLabelMap   = RESULT_MODE_LABELS;
  protected readonly skillLabelMap  = RESULT_SKILL_LABELS;
  protected readonly statusLabelMap = RESULT_STATUS_LABELS;

  ngOnInit(): void {
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
    this.filterMode.set('');
    this.filterSkill.set('');
    this.filterStatus.set('');
    this.filterStudent.set('');
    this.currentPage.set(1);
    void this.loadResults();
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

  protected onSelectRow(row: ResultRowDto): void {
    this.selectedRowId.set(row.id);
  }

  protected onPageChange(newPage: number): void {
    this.currentPage.set(newPage);
    void this.loadResults();
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString('vi-VN');
  }

  protected formatScore(score: number | null, type: string): string {
    if (score === null) return '—';
    if (type === 'speaking') return String(Math.round(score));
    return score.toFixed(1);
  }

  private async loadResults(): Promise<void> {
    const requestId = ++this.currentRequestId;
    this.loadState.set('loading');
    this.errorMessage.set(null);
    this.selectedRowId.set(null);

    const filter: ResultsFilter = {
      mode:      (this.filterMode()    || undefined) as ResultsFilter['mode'],
      skill:     (this.filterSkill()   || undefined) as ResultsFilter['skill'],
      status:    this.filterStatus()   || undefined,
      q:         this.filterStudent()  || undefined,
      page:      this.currentPage(),
      pageSize:  this.pageSize(),
      sort:      'submittedAt',
      direction: 'desc',
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
}
