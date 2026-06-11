import { Component, DestroyRef, HostListener, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import {
  SKILL_LABELS,
  STATUS_LABELS,
  TEMPLATE_ERROR_MESSAGES,
  TemplateSkill,
  TemplateStatus,
  TestTemplateDetail,
  TestTemplateListFilters,
  TestTemplateListItem,
} from '../../core/test-templates/test-templates.models';

@Component({
  selector: 'app-test-template-library',
  imports: [RouterLink],
  templateUrl: './test-template-library.component.html',
  styleUrl: './test-template-library.component.css',
})
export class TestTemplateLibraryComponent implements OnInit {
  private readonly api = inject(TestTemplatesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();
  private loadRequestId = 0;

  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly templates = signal<TestTemplateListItem[]>([]);
  protected readonly filters = signal<TestTemplateListFilters>({
    skill: '',
    status: '',
    q: '',
  });
  protected readonly openMenuId = signal<string | null>(null);
  protected readonly blockedActionMessage = signal<string | null>(null);
  protected readonly inspectedTemplate = signal<TestTemplateDetail | null>(null);
  protected readonly inspectLoading = signal(false);
  protected readonly inspectError = signal<string | null>(null);

  protected readonly skillOptions: { value: TemplateSkill; label: string }[] = [
    { value: '', label: 'Tất cả kỹ năng' },
    { value: 'reading', label: 'Reading' },
    { value: 'listening', label: 'Listening' },
    { value: 'speaking', label: 'Speaking' },
  ];

  protected readonly statusOptions: { value: TemplateStatus; label: string }[] = [
    { value: '', label: 'Tất cả trạng thái' },
    { value: 'draft', label: 'Nháp' },
    { value: 'ready', label: 'Sẵn sàng' },
    { value: 'archived', label: 'Đã lưu trữ' },
  ];

  @HostListener('document:keydown.escape')
  protected onEscapeKey(): void {
    this.closeMenus();
  }

  async ngOnInit(): Promise<void> {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((query) => {
        this.filters.update((current) => ({ ...current, q: query }));
        void this.syncQueryParamsAndLoad();
      });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const skill = (params.get('skill') ?? '') as TemplateSkill;
      const status = (params.get('status') ?? '') as TemplateStatus;
      const q = params.get('q') ?? '';
      this.filters.set({ skill, status, q });
      void this.loadTemplates();
    });
  }

  protected skillLabel(skill: string): string {
    return SKILL_LABELS[skill] ?? skill;
  }

  protected statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  protected lastUsedLabel(value: string | null): string {
    if (!value) {
      return '—';
    }

    return new Date(value).toLocaleDateString('vi-VN');
  }

  protected hasActiveFilters(): boolean {
    const current = this.filters();
    return Boolean(current.skill || current.status || current.q.trim());
  }

  protected isReady(template: TestTemplateListItem): boolean {
    return template.status === 'ready';
  }

  protected onSkillChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as TemplateSkill;
    this.filters.update((current) => ({ ...current, skill: value }));
    void this.syncQueryParamsAndLoad();
  }

  protected onStatusChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as TemplateStatus;
    this.filters.update((current) => ({ ...current, status: value }));
    void this.syncQueryParamsAndLoad();
  }

  protected onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput$.next(value);
  }

  protected async clearFilters(): Promise<void> {
    this.filters.set({ skill: '', status: '', q: '' });
    await this.syncQueryParamsAndLoad();
  }

  protected toggleMenu(templateId: string, event: Event): void {
    event.stopPropagation();
    this.openMenuId.update((current) => (current === templateId ? null : templateId));
    this.blockedActionMessage.set(null);
  }

  protected closeMenus(): void {
    this.openMenuId.set(null);
  }

  protected async inspectTemplate(template: TestTemplateListItem, event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    this.closeMenus();

    if (template.status === 'draft') {
      await this.router.navigate(['/teacher/library', template.templateId, 'setup']);
      return;
    }

    this.inspectLoading.set(true);
    this.inspectError.set(null);

    try {
      const detail = await this.api.getTemplate(template.templateId);
      this.inspectedTemplate.set(detail);
    } catch {
      this.inspectedTemplate.set(null);
      this.inspectError.set('Không thể tải chi tiết đề. Vui lòng thử lại.');
    } finally {
      this.inspectLoading.set(false);
    }
  }

  protected closeInspectPanel(): void {
    this.inspectedTemplate.set(null);
    this.inspectError.set(null);
  }

  protected onHomeworkAction(template: TestTemplateListItem, event: Event): void {
    event.preventDefault();
    if (!this.isReady(template)) {
      this.blockedActionMessage.set(TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NOT_READY']);
      return;
    }

    this.blockedActionMessage.set(null);
    void this.router.navigate(['/teacher/homework/new'], {
      queryParams: { templateId: template.templateId },
    });
  }

  protected onLiveExamAction(template: TestTemplateListItem, event: Event): void {
    event.preventDefault();
    if (!this.isReady(template)) {
      this.blockedActionMessage.set(TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NOT_READY']);
      return;
    }

    this.blockedActionMessage.set(null);
    void this.router.navigate(['/teacher/live-exams/new'], {
      queryParams: { templateId: template.templateId },
    });
  }

  protected async retryLoad(): Promise<void> {
    await this.loadTemplates();
  }

  private async syncQueryParamsAndLoad(): Promise<void> {
    const current = this.filters();
    await this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        skill: current.skill || null,
        status: current.status || null,
        q: current.q.trim() || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private async loadTemplates(): Promise<void> {
    const requestId = ++this.loadRequestId;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const items = await this.api.listTemplates(this.filters());
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.templates.set(items);
      this.closeInspectPanel();
    } catch {
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.errorMessage.set('Không thể tải thư viện đề. Vui lòng thử lại.');
    } finally {
      if (requestId === this.loadRequestId) {
        this.isLoading.set(false);
      }
    }
  }
}
