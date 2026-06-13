import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TeacherResultsComponent } from './teacher-results.component';
import { ResultsApiService } from '../../core/results/results-api.service';
import { ResultsPageDto } from '../../core/results/results.models';

const mockEmptyPage: ResultsPageDto = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  needsGrading: 0,
};

const mockPageWithItems: ResultsPageDto = {
  items: [
    {
      id: 'sub-1',
      type: 'reading-listening',
      mode: 'homework',
      studentName: 'Test Student',
      studentId: 'student-id-1',
      classId: 'class-1',
      className: 'English 7A',
      templateId: 'tpl-1',
      templateTitle: 'Reading Test',
      skill: 'reading',
      status: 'submitted',
      score: 7.5,
      submittedAt: '2026-06-13T10:00:00Z',
      createdAt: '2026-06-13T09:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  needsGrading: 0,
};

const mockPageWithNeedsGrading: ResultsPageDto = {
  items: [
    {
      id: 'speak-1',
      type: 'speaking',
      mode: 'homework',
      studentName: 'Test Student',
      studentId: 'student-id-1',
      classId: 'class-1',
      className: 'English 7A',
      templateId: 'tpl-2',
      templateTitle: 'Speaking Test',
      skill: 'speaking',
      status: 'submitted',
      score: null,
      submittedAt: '2026-06-13T10:00:00Z',
      createdAt: '2026-06-13T09:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  needsGrading: 1,
};

function createMockApiService(page: ResultsPageDto) {
  return { getResults: vi.fn().mockResolvedValue(page) };
}

async function createComponent(mockPage: ResultsPageDto): Promise<ComponentFixture<TeacherResultsComponent>> {
  const mockService = createMockApiService(mockPage);
  await TestBed.configureTestingModule({
    imports: [TeacherResultsComponent],
    providers: [{ provide: ResultsApiService, useValue: mockService }],
  }).compileComponents();

  const fixture = TestBed.createComponent(TeacherResultsComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return fixture;
}

describe('TeacherResultsComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('shows results table with items on successful load', async () => {
    const fixture = await createComponent(mockPageWithItems);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.results-table')).toBeTruthy();
    expect(el.querySelectorAll('tbody tr').length).toBe(1);
    expect(el.textContent).toContain('Test Student');
    expect(el.textContent).toContain('Reading Test');
  });

  it('shows empty state when items is empty', async () => {
    const fixture = await createComponent(mockEmptyPage);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.empty-state')).toBeTruthy();
    expect(el.querySelector('.results-table')).toBeFalsy();
  });

  it('shows needs-grading badge when needsGrading > 0', async () => {
    const fixture = await createComponent(mockPageWithNeedsGrading);
    const el: HTMLElement = fixture.nativeElement;
    const badge = el.querySelector('.needs-grading-badge');
    expect(badge).toBeTruthy();
    expect(badge?.textContent).toContain('1');
  });

  it('calls getResults again when onClearFilters is invoked', async () => {
    const mockService = createMockApiService(mockEmptyPage);
    await TestBed.configureTestingModule({
      imports: [TeacherResultsComponent],
      providers: [{ provide: ResultsApiService, useValue: mockService }],
    }).compileComponents();

    const fixture = TestBed.createComponent(TeacherResultsComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const initialCallCount = mockService.getResults.mock.calls.length;

    // Invoke clear filters
    const component = fixture.componentInstance as unknown as { onClearFilters: () => void };
    component.onClearFilters();
    await fixture.whenStable();

    expect(mockService.getResults.mock.calls.length).toBeGreaterThan(initialCallCount);
  });

  it('shows error state when API throws', async () => {
    const mockService = { getResults: vi.fn().mockRejectedValue(new Error('network error')) };
    await TestBed.configureTestingModule({
      imports: [TeacherResultsComponent],
      providers: [{ provide: ResultsApiService, useValue: mockService }],
    }).compileComponents();

    const fixture = TestBed.createComponent(TeacherResultsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.error-state')).toBeTruthy();
    expect(el.querySelector('.results-table')).toBeFalsy();
  });
});
