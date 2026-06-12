import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { StudentAttemptWorkspaceComponent } from './student-attempt-workspace.component';
import { SubmissionsApiService } from '../../core/submissions/submissions-api.service';
import { SubmissionWorkspace } from '../../core/submissions/submissions.models';

async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeWorkspace(overrides: Partial<SubmissionWorkspace> = {}): SubmissionWorkspace {
  return {
    id: 'sub-1',
    status: 'draft',
    mode: 'homework',
    templateTitle: 'Unit 1 Reading Test',
    skill: 'reading',
    classId: 'cls-1',
    className: 'Lớp 7A',
    homeworkAssignmentId: 'hw-1',
    liveExamSessionId: null,
    deadlineAt: '2026-12-31T12:00:00Z',
    timeLimitMinutes: null,
    sessionOpenedAt: null,
    sessionClosedAt: null,
    pdfMaterialId: 'file-pdf-1',
    audioMaterialId: null,
    questionCount: 3,
    answerRows: [],
    ...overrides,
  };
}

describe('StudentAttemptWorkspaceComponent', () => {
  let fixture: ComponentFixture<StudentAttemptWorkspaceComponent>;
  let component: StudentAttemptWorkspaceComponent;
  let submissionsApi: {
    getWorkspace: ReturnType<typeof vi.fn>;
    getMaterialContentUrl: ReturnType<typeof vi.fn>;
  };

  async function setup(
    submissionId: string | null,
    workspace: SubmissionWorkspace | null = makeWorkspace(),
  ): Promise<void> {
    submissionsApi = {
      getWorkspace: workspace
        ? vi.fn().mockResolvedValue(workspace)
        : vi.fn().mockRejectedValue(new Error('not found')),
      getMaterialContentUrl: vi.fn(
        (subId: string, fileId: string) =>
          `/api/submissions/${subId}/materials/${fileId}/content`,
      ),
    };

    await TestBed.configureTestingModule({
      imports: [StudentAttemptWorkspaceComponent],
      providers: [
        provideRouter([]),
        { provide: SubmissionsApiService, useValue: submissionsApi },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(submissionId ? { submissionId } : {}),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentAttemptWorkspaceComponent);
    component = fixture.componentInstance;
  }

  async function initAndLoad(): Promise<void> {
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();
  }

  it('hiển thị loading state khi khởi tạo', async () => {
    submissionsApi = {
      getWorkspace: vi.fn().mockReturnValue(new Promise(() => {})),
      getMaterialContentUrl: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StudentAttemptWorkspaceComponent],
      providers: [
        provideRouter([]),
        { provide: SubmissionsApiService, useValue: submissionsApi },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ submissionId: 'sub-1' }) } },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(StudentAttemptWorkspaceComponent);
    component = fixture.componentInstance;

    fixture.detectChanges();

    const loading = fixture.nativeElement.querySelector('.loading-state');
    expect(loading).toBeTruthy();
  });

  it('sau khi load thành công, hiển thị tiêu đề và skill badge', async () => {
    await setup('sub-1', makeWorkspace({ templateTitle: 'Unit 2 Listening', skill: 'listening' }));
    await initAndLoad();

    expect((component as any).viewState()).toBe('loaded');
    const title = fixture.nativeElement.querySelector('.template-title');
    expect(title?.textContent).toContain('Unit 2 Listening');
    const skillBadge = fixture.nativeElement.querySelector('.skill-badge');
    expect(skillBadge).toBeTruthy();
  });

  it('hiển thị error state khi API thất bại', async () => {
    await setup('sub-1', null);
    await initAndLoad();

    expect((component as any).viewState()).toBe('error');
    const error = fixture.nativeElement.querySelector('.error-state');
    expect(error).toBeTruthy();
    const retryBtn = fixture.nativeElement.querySelector('[class*="primary-button"]');
    expect(retryBtn).toBeTruthy();
  });

  it('gán pdfUrl từ getMaterialContentUrl', async () => {
    await setup('sub-1', makeWorkspace({ pdfMaterialId: 'file-123' }));
    await initAndLoad();

    expect(submissionsApi.getMaterialContentUrl).toHaveBeenCalledWith('sub-1', 'file-123');
    expect((component as any).pdfUrl()).not.toBeNull();
  });

  it('workspace Reading — không hiển thị audio player', async () => {
    await setup('sub-1', makeWorkspace({ skill: 'reading', audioMaterialId: 'audio-99' }));
    await initAndLoad();

    expect((component as any).audioUrl()).toBeNull();
    const audioEl = fixture.nativeElement.querySelector('[data-testid="audio-player"]');
    expect(audioEl).toBeNull();
  });

  it('workspace Listening với audio — hiển thị audio player', async () => {
    await setup('sub-1', makeWorkspace({ skill: 'listening', audioMaterialId: 'audio-99' }));
    await initAndLoad();

    expect(submissionsApi.getMaterialContentUrl).toHaveBeenCalledWith('sub-1', 'audio-99');
    expect((component as any).audioUrl()).not.toBeNull();
    const audioEl = fixture.nativeElement.querySelector('[data-testid="audio-player"]');
    expect(audioEl).toBeTruthy();
  });

  it('answerRange() trả về mảng 1..questionCount', async () => {
    await setup('sub-1', makeWorkspace({ questionCount: 5 }));
    await initAndLoad();

    const nums = (component as any).answerRange();
    expect(nums).toEqual([1, 2, 3, 4, 5]);
  });

  it('onAnswerChange cập nhật answerInputs signal', async () => {
    await setup('sub-1');
    await initAndLoad();

    (component as any).onAnswerChange(1, 'A');
    expect((component as any).answerInputs()[1]).toBe('A');
  });

  it('answeredCount tăng khi nhập câu trả lời', async () => {
    await setup('sub-1', makeWorkspace({ questionCount: 3 }));
    await initAndLoad();

    expect((component as any).answeredCount()).toBe(0);
    (component as any).onAnswerChange(1, 'A');
    expect((component as any).answeredCount()).toBe(1);
    (component as any).onAnswerChange(2, 'B');
    expect((component as any).answeredCount()).toBe(2);
  });

  it('submit button hiện diện và disabled', async () => {
    await setup('sub-1');
    await initAndLoad();

    const submitBtn = fixture.nativeElement.querySelector('[data-testid="submit-button"]');
    expect(submitBtn).toBeTruthy();
    expect(submitBtn.disabled).toBe(true);
  });

  it('autosave-status region hiện diện', async () => {
    await setup('sub-1');
    await initAndLoad();

    const autosave = fixture.nativeElement.querySelector('[data-testid="autosave-status"]');
    expect(autosave).toBeTruthy();
    expect(autosave.textContent).toContain('—');
  });

  it('answer-progress hiện diện và đúng', async () => {
    await setup('sub-1', makeWorkspace({ questionCount: 3 }));
    await initAndLoad();

    const progress = fixture.nativeElement.querySelector('[data-testid="answer-progress"]');
    expect(progress).toBeTruthy();
    expect(progress.textContent).toContain('0/3');
  });

  it('backToTests() điều hướng về /student/tests', async () => {
    await setup('sub-1');
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    await initAndLoad();

    (component as any).backToTests();

    expect(navigateSpy).toHaveBeenCalledWith(['/student/tests']);
  });

  it('retryLoad() gọi lại getWorkspace', async () => {
    await setup('sub-1', null);
    await initAndLoad();

    expect((component as any).viewState()).toBe('error');
    submissionsApi.getWorkspace.mockResolvedValue(makeWorkspace());
    (component as any).retryLoad();
    await flushPromises();
    fixture.detectChanges();

    expect(submissionsApi.getWorkspace).toHaveBeenCalledTimes(2);
    expect((component as any).viewState()).toBe('loaded');
  });

  it('điều hướng về /student/tests khi không có submissionId', async () => {
    await setup(null, makeWorkspace());
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.detectChanges();
    await flushPromises();

    expect(navigateSpy).toHaveBeenCalledWith(['/student/tests']);
  });
});
