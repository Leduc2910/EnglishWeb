import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DashboardApiService } from '../../core/dashboard/dashboard-api.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import {
  TeacherDashboardDto,
  TeacherRecentWorkItemDto,
  RECENT_WORK_MODE_LABELS,
  RECENT_WORK_STATUS_LABELS,
} from '../../core/dashboard/dashboard.models';
import { ClassSummary } from '../../core/classes/classes.models';

@Component({
  selector: 'app-teacher-dashboard',
  templateUrl: './teacher-dashboard.component.html',
  styleUrl: './teacher-dashboard.component.css',
  imports: [FormsModule, RouterLink, DatePipe],
})
export class TeacherDashboardComponent implements OnInit {
  private readonly dashboardApi = inject(DashboardApiService);
  private readonly classesApi   = inject(ClassesApiService);

  protected readonly dashboard        = signal<TeacherDashboardDto | null>(null);
  protected readonly availableClasses = signal<ClassSummary[]>([]);
  protected readonly filterClass      = signal<string>('');
  protected readonly loadState        = signal<'loading' | 'loaded' | 'error'>('loading');
  protected readonly loadError        = signal<string | null>(null);

  protected readonly modeLabelMap   = RECENT_WORK_MODE_LABELS;
  protected readonly statusLabelMap = RECENT_WORK_STATUS_LABELS;

  ngOnInit(): void {
    void this.loadClasses();
    void this.loadDashboard();
  }

  private async loadClasses(): Promise<void> {
    try {
      this.availableClasses.set(await this.classesApi.getTeacherClasses());
    } catch {
      // non-critical — filter just won't populate
    }
  }

  protected async onClassFilterChange(): Promise<void> {
    await this.loadDashboard();
  }

  private async loadDashboard(): Promise<void> {
    this.loadState.set('loading');
    this.loadError.set(null);
    try {
      const classId = this.filterClass() || undefined;
      const data = await this.dashboardApi.getDashboard(classId);
      this.dashboard.set(data);
      this.loadState.set('loaded');
    } catch {
      this.loadState.set('error');
      this.loadError.set('Không thể tải dữ liệu. Vui lòng thử lại.');
    }
  }

  protected getRouterLink(item: TeacherRecentWorkItemDto): string[] {
    if (item.type === 'template') return ['/teacher/library', item.id, 'review'];
    return ['/teacher/results'];
  }

  protected getQueryParams(item: TeacherRecentWorkItemDto): Record<string, string> {
    if (item.mode === 'homework') return { mode: 'homework' };
    if (item.mode === 'live-exam') return { mode: 'live-exam' };
    return {};
  }
}
