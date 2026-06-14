import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { provideRouter } from '@angular/router';
import { TeacherDashboardComponent } from './teacher-dashboard.component';
import { DashboardApiService } from '../../core/dashboard/dashboard-api.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { TeacherDashboardDto } from '../../core/dashboard/dashboard.models';

const mockSummaryData: TeacherDashboardDto = {
  summary: {
    templateCount: 3,
    activeHomeworkCount: 1,
    openLiveExamCount: 0,
    recentSubmissionCount: 5,
    pendingSpeakingCount: 2,
  },
  recentWork: [
    {
      type: 'submission',
      id: 'sub-1',
      title: 'Reading Test',
      className: 'English 7A',
      mode: 'homework',
      status: 'submitted',
      timestamp: '2026-06-13T10:00:00Z',
    },
  ],
};

const mockEmptyData: TeacherDashboardDto = {
  summary: {
    templateCount: 0,
    activeHomeworkCount: 0,
    openLiveExamCount: 0,
    recentSubmissionCount: 0,
    pendingSpeakingCount: 0,
  },
  recentWork: [],
};

const mockDashboardApi = {
  getDashboard: vi.fn(),
};

const mockClassesApi = {
  getTeacherClasses: vi.fn().mockResolvedValue([]),
};

describe('TeacherDashboardComponent', () => {
  let fixture: ComponentFixture<TeacherDashboardComponent>;
  let component: TeacherDashboardComponent;

  beforeEach(async () => {
    mockDashboardApi.getDashboard.mockResolvedValue(mockSummaryData);
    mockClassesApi.getTeacherClasses.mockResolvedValue([]);

    await TestBed.configureTestingModule({
      imports: [TeacherDashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardApiService, useValue: mockDashboardApi },
        { provide: ClassesApiService, useValue: mockClassesApi },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TeacherDashboardComponent);
    component = fixture.componentInstance;
  });

  it('displays summary metrics after load', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('#teacher-dashboard-loading')).toBeNull();
    expect(el.querySelector('#teacher-dashboard-templates-card')).not.toBeNull();
    expect(el.querySelector('.metric')?.textContent?.trim()).toBe('3');
  });

  it('displays empty state when recentWork is empty', async () => {
    mockDashboardApi.getDashboard.mockResolvedValue(mockEmptyData);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('#teacher-dashboard-empty-state')).not.toBeNull();
    expect(el.querySelector('.recent-table')).toBeNull();
  });

  it('displays recent work rows when data exists', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.recent-table')).not.toBeNull();
    const rows = el.querySelectorAll('.recent-row');
    expect(rows.length).toBe(1);
  });

  it('reloads dashboard when class filter changes', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    mockDashboardApi.getDashboard.mockClear();
    mockDashboardApi.getDashboard.mockResolvedValue(mockEmptyData);

    await (component as unknown as { onClassFilterChange: () => Promise<void> }).onClassFilterChange();
    expect(mockDashboardApi.getDashboard).toHaveBeenCalledTimes(1);
  });

  it('recent-table is wrapped in recent-table-wrapper for responsive overflow', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const wrapper = el.querySelector('.recent-table-wrapper');
    expect(wrapper).not.toBeNull();
    const table = wrapper?.querySelector('.recent-table');
    expect(table).not.toBeNull();
  });

  it('filter-bar select has label association', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const select = el.querySelector('.filter-bar select') as HTMLSelectElement;
    expect(select).not.toBeNull();
    const id = select?.id;
    const label = el.querySelector(`label[for="${id}"]`);
    expect(label).not.toBeNull();
  });
});
