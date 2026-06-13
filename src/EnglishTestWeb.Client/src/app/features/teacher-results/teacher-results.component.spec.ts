import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TeacherResultsComponent } from './teacher-results.component';
import { ResultsApiService } from '../../core/results/results-api.service';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { ResultRowDto, ResultsPageDto, TeacherSubmissionDetailDto } from '../../core/results/results.models';
import { TeacherSpeakingSubmissionDto } from '../../core/speaking/speaking.models';

const mockEmptyPage: ResultsPageDto = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  needsGrading: 0,
};

const mockRLRow: ResultRowDto = {
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
};

const mockSpeakingRow: ResultRowDto = {
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
};

const mockPageWithRLItem: ResultsPageDto = {
  items: [mockRLRow],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  needsGrading: 0,
};

const mockPageWithSpeakingItem: ResultsPageDto = {
  items: [mockSpeakingRow],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  needsGrading: 1,
};

const mockRLDetail: TeacherSubmissionDetailDto = {
  id: 'sub-1',
  studentName: 'Test Student',
  className: 'English 7A',
  templateTitle: 'Reading Test',
  skill: 'reading',
  mode: 'homework',
  status: 'submitted',
  autoScore: 7.5,
  submittedAt: '2026-06-13T10:00:00Z',
  answers: [
    { questionNumber: 1, studentAnswer: 'A', correctAnswer: 'A', isCorrect: true, score: 1 },
    { questionNumber: 2, studentAnswer: 'B', correctAnswer: 'C', isCorrect: false, score: 0 },
  ],
};

const mockSpeakingDto: TeacherSpeakingSubmissionDto = {
  id: 'speak-1',
  studentName: 'Test Student',
  className: 'English 7A',
  templateTitle: 'Speaking Test',
  mode: 'homework',
  status: 'submitted',
  submittedAt: '2026-06-13T10:00:00Z',
  submittedFileName: 'audio.mp3',
  submittedFileSizeBytes: 1024,
  submittedFileId: 'file-1',
  isFileMissing: false,
  score: null,
  feedback: null,
  graderId: null,
  gradedAt: null,
};

function createMockServices(page: ResultsPageDto, detailDto?: TeacherSubmissionDetailDto) {
  return {
    resultsApi: {
      getResults: vi.fn().mockResolvedValue(page),
      getSubmissionDetail: vi.fn().mockResolvedValue(detailDto ?? mockRLDetail),
    },
    speakingApi: {
      getForTeacher: vi.fn().mockResolvedValue(mockSpeakingDto),
      grade: vi.fn().mockResolvedValue({ ...mockSpeakingDto, status: 'graded', score: 8 }),
      getTeacherSubmissionFileUrl: vi.fn().mockReturnValue('/api/mock-audio'),
    },
    classesApi: { getTeacherClasses: vi.fn().mockResolvedValue([]) },
    templatesApi: { listTemplates: vi.fn().mockResolvedValue([]) },
  };
}

async function createComponent(
  mockPage: ResultsPageDto,
  detailDto?: TeacherSubmissionDetailDto,
): Promise<{ fixture: ComponentFixture<TeacherResultsComponent>; mocks: ReturnType<typeof createMockServices> }> {
  const mocks = createMockServices(mockPage, detailDto);

  await TestBed.configureTestingModule({
    imports: [TeacherResultsComponent],
    providers: [
      { provide: ResultsApiService, useValue: mocks.resultsApi },
      { provide: SpeakingApiService, useValue: mocks.speakingApi },
      { provide: ClassesApiService, useValue: mocks.classesApi },
      { provide: TestTemplatesApiService, useValue: mocks.templatesApi },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(TeacherResultsComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, mocks };
}

describe('TeacherResultsComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('shows results table with items on successful load', async () => {
    const { fixture } = await createComponent(mockPageWithRLItem);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.results-table')).toBeTruthy();
    expect(el.querySelectorAll('tbody tr').length).toBe(1);
    expect(el.textContent).toContain('Test Student');
    expect(el.textContent).toContain('Reading Test');
  });

  it('shows empty state when items is empty', async () => {
    const { fixture } = await createComponent(mockEmptyPage);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.empty-state')).toBeTruthy();
    expect(el.querySelector('.results-table')).toBeFalsy();
  });

  it('shows needs-grading badge when needsGrading > 0', async () => {
    const { fixture } = await createComponent(mockPageWithSpeakingItem);
    const el: HTMLElement = fixture.nativeElement;
    const badge = el.querySelector('.needs-grading-badge');
    expect(badge).toBeTruthy();
    expect(badge?.textContent).toContain('1');
  });

  it('calls getResults again when onClearFilters is invoked', async () => {
    const { fixture, mocks } = await createComponent(mockEmptyPage);
    const initialCallCount = mocks.resultsApi.getResults.mock.calls.length;
    const component = fixture.componentInstance as unknown as { onClearFilters: () => void };
    component.onClearFilters();
    await fixture.whenStable();
    expect(mocks.resultsApi.getResults.mock.calls.length).toBeGreaterThan(initialCallCount);
  });

  it('shows error state when API throws', async () => {
    const mocks = createMockServices(mockEmptyPage);
    mocks.resultsApi.getResults = vi.fn().mockRejectedValue(new Error('network error'));

    await TestBed.configureTestingModule({
      imports: [TeacherResultsComponent],
      providers: [
        { provide: ResultsApiService, useValue: mocks.resultsApi },
        { provide: SpeakingApiService, useValue: mocks.speakingApi },
        { provide: ClassesApiService, useValue: mocks.classesApi },
        { provide: TestTemplatesApiService, useValue: mocks.templatesApi },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(TeacherResultsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.error-state')).toBeTruthy();
    expect(el.querySelector('.results-table')).toBeFalsy();
  });

  it('opens detail panel when a row is selected', async () => {
    const { fixture } = await createComponent(mockPageWithRLItem, mockRLDetail);
    const el: HTMLElement = fixture.nativeElement;
    const row = el.querySelector('tbody tr') as HTMLElement;
    row.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(el.querySelector('.detail-panel')).toBeTruthy();
  });

  it('closes detail panel when close button is clicked', async () => {
    const { fixture } = await createComponent(mockPageWithRLItem, mockRLDetail);
    const el: HTMLElement = fixture.nativeElement;

    // Open detail
    (el.querySelector('tbody tr') as HTMLElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(el.querySelector('.detail-panel')).toBeTruthy();

    // Close detail
    (el.querySelector('.close-btn') as HTMLElement).click();
    fixture.detectChanges();
    expect(el.querySelector('.detail-panel')).toBeFalsy();
  });

  it('calls grade API and updates row status on grade submit', async () => {
    const { fixture, mocks } = await createComponent(mockPageWithSpeakingItem);
    const el: HTMLElement = fixture.nativeElement;

    // Open detail
    (el.querySelector('tbody tr') as HTMLElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    // Set score input
    const component = fixture.componentInstance as unknown as {
      scoreInput: { set: (v: string) => void };
      onGradeSubmit: () => Promise<void>;
    };
    component.scoreInput.set('8');
    await component.onGradeSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(mocks.speakingApi.grade).toHaveBeenCalledWith('speak-1', { score: 8, feedback: null });

    // Row status should be updated to 'graded' in the table
    const statusBadge = el.querySelector('.status-badge');
    expect(statusBadge?.textContent?.trim()).toContain('Đã chấm');
  });

  it('resets filterClass and filterTemplate signals on onClearFilters', async () => {
    const { fixture } = await createComponent(mockEmptyPage);
    const component = fixture.componentInstance as unknown as {
      filterClass: { set: (v: string) => void; (): string };
      filterTemplate: { set: (v: string) => void; (): string };
      onClearFilters: () => void;
    };

    component.filterClass.set('some-class-id');
    component.filterTemplate.set('some-template-id');
    component.onClearFilters();

    expect(component.filterClass()).toBe('');
    expect(component.filterTemplate()).toBe('');
  });

  it('status-submitted badge uses status-submitted CSS class (blue, not amber)', async () => {
    const { fixture } = await createComponent(mockPageWithRLItem);
    const el: HTMLElement = fixture.nativeElement;
    const badge = el.querySelector('.status-badge');
    expect(badge).toBeTruthy();
    expect(badge?.classList.contains('status-submitted')).toBe(true);
    expect(badge?.classList.contains('status-amber')).toBe(false);
  });

  it('filter bar select elements exist for keyboard focus accessibility', async () => {
    const { fixture } = await createComponent(mockEmptyPage);
    const el: HTMLElement = fixture.nativeElement;
    const filterBar = el.querySelector('.filter-bar');
    expect(filterBar).toBeTruthy();
    const selects = filterBar?.querySelectorAll('select');
    expect(selects?.length).toBeGreaterThan(0);
  });
});
