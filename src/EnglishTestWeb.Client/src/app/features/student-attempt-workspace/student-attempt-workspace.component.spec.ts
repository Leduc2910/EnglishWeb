import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { StudentAttemptWorkspaceComponent } from './student-attempt-workspace.component';
import { SubmissionsApiService } from '../../core/submissions/submissions-api.service';
import { SubmissionResultDto, SubmissionWorkspace } from '../../core/submissions/submissions.models';

async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeSubmitResult(overrides: Partial<SubmissionResultDto> = {}): SubmissionResultDto {
  return {
    submissionId: 'sub-1',
    status: 'auto-graded',
    mode: 'homework',
    templateTitle: 'Unit 1 Reading Test',
    submittedAt: '2026-06-12T10:00:00Z',
    autoScore: 10,
    questionCount: 1,
    correctCount: 1,
    ...overrides,
  };
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
    autosaveAnswers: ReturnType<typeof vi.fn>;
    finalSubmit: ReturnType<typeof vi.fn>;
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
      autosaveAnswers: vi.fn().mockResolvedValue(undefined),
      finalSubmit: vi.fn().mockResolvedValue(makeSubmitResult()),
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
      autosaveAnswers: vi.fn().mockResolvedValue(undefined),
      finalSubmit: vi.fn().mockResolvedValue(makeSubmitResult()),
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

  it('submit button hiện diện và enabled khi workspace status = draft', async () => {
    await setup('sub-1');
    await initAndLoad();

    const submitBtn = fixture.nativeElement.querySelector('[data-testid="submit-button"]');
    expect(submitBtn).toBeTruthy();
    expect(submitBtn.disabled).toBe(false);
  });

  it('autosave-status region hiện diện với aria-live', async () => {
    await setup('sub-1');
    await initAndLoad();

    const autosave = fixture.nativeElement.querySelector('[data-testid="autosave-status"]');
    expect(autosave).toBeTruthy();
    expect(autosave.getAttribute('aria-live')).toBe('polite');
  });

  it('autosave-status hiển thị idle khi chưa nhập', async () => {
    await setup('sub-1');
    await initAndLoad();

    const autosave = fixture.nativeElement.querySelector('[data-testid="autosave-status"]');
    expect(autosave.textContent).toContain('—');
  });

  it('performAutosave thành công → autosaveStatus = saved', async () => {
    await setup('sub-1');
    await initAndLoad();

    submissionsApi.autosaveAnswers.mockResolvedValue(undefined);
    await (component as any).performAutosave();

    expect((component as any).autosaveStatus()).toBe('saved');
  });

  it('performAutosave thất bại → autosaveStatus = error', async () => {
    await setup('sub-1');
    await initAndLoad();

    submissionsApi.autosaveAnswers.mockRejectedValue(new Error('network error'));
    await (component as any).performAutosave();

    expect((component as any).autosaveStatus()).toBe('error');
  });

  it('load workspace có answerRows → answerInputs được khôi phục', async () => {
    const wsWithAnswers = makeWorkspace({
      answerRows: [
        { questionNumber: 1, answer: 'A' },
        { questionNumber: 2, answer: 'C' },
      ],
    });
    await setup('sub-1', wsWithAnswers);
    await initAndLoad();

    const inputs = (component as any).answerInputs();
    expect(inputs[1]).toBe('A');
    expect(inputs[2]).toBe('C');
    expect((component as any).answeredCount()).toBe(2);
  });

  it('load workspace có answerRows null → bỏ qua, không crash', async () => {
    const wsWithNull = makeWorkspace({
      answerRows: [
        { questionNumber: 1, answer: null },
        { questionNumber: 2, answer: 'B' },
      ],
    });
    await setup('sub-1', wsWithNull);
    await initAndLoad();

    const inputs = (component as any).answerInputs();
    expect(inputs[1]).toBeUndefined();
    expect(inputs[2]).toBe('B');
  });

  it('workspace status=submitted → performAutosave không gọi autosaveAnswers', async () => {
    const submittedWs = makeWorkspace({ status: 'submitted' });
    await setup('sub-1', submittedWs);
    await initAndLoad();

    await (component as any).performAutosave();

    expect(submissionsApi.autosaveAnswers).not.toHaveBeenCalled();
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

  it('submit button enabled khi workspace status = draft', async () => {
    await setup('sub-1', makeWorkspace({ status: 'draft' }));
    await initAndLoad();

    const submitBtn = fixture.nativeElement.querySelector('[data-testid="submit-button"]');
    expect(submitBtn).toBeTruthy();
    expect(submitBtn.disabled).toBe(false);
  });

  it('onSubmit() mở confirmation modal khi status = draft', async () => {
    await setup('sub-1', makeWorkspace({ status: 'draft' }));
    await initAndLoad();

    (component as any).onSubmit();
    fixture.detectChanges();

    expect((component as any).isSubmitConfirmOpen()).toBe(true);
    const modal = fixture.nativeElement.querySelector('[data-testid="submit-confirm-modal"]');
    expect(modal).toBeTruthy();
  });

  it('confirmation modal hiển thị missing count khi có câu chưa điền', async () => {
    await setup('sub-1', makeWorkspace({ questionCount: 3 }));
    await initAndLoad();

    (component as any).onSubmit();
    fixture.detectChanges();

    const missingEl = fixture.nativeElement.querySelector('[data-testid="confirm-missing-count"]');
    expect(missingEl).toBeTruthy();
    expect(missingEl.textContent).toContain('3');
  });

  it('confirmation modal hiển thị "đủ câu" khi tất cả đã điền', async () => {
    await setup('sub-1', makeWorkspace({ questionCount: 1 }));
    await initAndLoad();

    (component as any).onAnswerChange(1, 'A');
    (component as any).onSubmit();
    fixture.detectChanges();

    const completeEl = fixture.nativeElement.querySelector('[data-testid="confirm-all-answered"]');
    expect(completeEl).toBeTruthy();
  });

  it('onCancelSubmit() đóng modal (isSubmitConfirmOpen = false)', async () => {
    await setup('sub-1', makeWorkspace({ status: 'draft' }));
    await initAndLoad();

    (component as any).onSubmit();
    fixture.detectChanges();
    expect((component as any).isSubmitConfirmOpen()).toBe(true);

    (component as any).onCancelSubmit();
    fixture.detectChanges();
    expect((component as any).isSubmitConfirmOpen()).toBe(false);
  });

  it('onConfirmSubmit() gọi finalSubmit và hiển thị success state', async () => {
    await setup('sub-1', makeWorkspace({ status: 'draft' }));
    await initAndLoad();

    await (component as any).onConfirmSubmit();
    fixture.detectChanges();

    expect(submissionsApi.finalSubmit).toHaveBeenCalledWith('sub-1');
    expect((component as any).submitResult()).toBeTruthy();
    const successEl = fixture.nativeElement.querySelector('[data-testid="submit-success"]');
    expect(successEl).toBeTruthy();
  });

  it('success state hiển thị templateTitle, mode, submittedAt', async () => {
    await setup('sub-1', makeWorkspace({ status: 'draft' }));
    await initAndLoad();

    await (component as any).onConfirmSubmit();
    fixture.detectChanges();

    const titleEl = fixture.nativeElement.querySelector('[data-testid="result-template-title"]');
    expect(titleEl?.textContent).toContain('Unit 1 Reading Test');
    const modeEl = fixture.nativeElement.querySelector('[data-testid="result-mode"]');
    expect(modeEl).toBeTruthy();
    const dateEl = fixture.nativeElement.querySelector('[data-testid="result-submitted-at"]');
    expect(dateEl).toBeTruthy();
  });

  it('onConfirmSubmit() thất bại → submitState = error, submit-error hiển thị', async () => {
    await setup('sub-1', makeWorkspace({ status: 'draft' }));
    await initAndLoad();

    submissionsApi.finalSubmit.mockRejectedValue({ error: { extensions: { code: 'submission.sourceUnavailable' } } });
    await (component as any).onConfirmSubmit();
    fixture.detectChanges();

    expect((component as any).submitState()).toBe('error');
    const errEl = fixture.nativeElement.querySelector('[data-testid="submit-error"]');
    expect(errEl).toBeTruthy();
  });

  it('inputs bị disabled khi workspace.status = submitted', async () => {
    await setup('sub-1', makeWorkspace({ status: 'submitted', questionCount: 1 }));
    await initAndLoad();

    const input = fixture.nativeElement.querySelector('[data-testid="answer-input-1"]');
    expect(input?.disabled).toBe(true);
  });

  it('submit button disabled khi workspace.status = submitted', async () => {
    await setup('sub-1', makeWorkspace({ status: 'submitted' }));
    await initAndLoad();

    const submitBtn = fixture.nativeElement.querySelector('[data-testid="submit-button"]');
    expect(submitBtn?.disabled).toBe(true);
  });

  it('performAutosave không gọi khi workspace.status = auto-graded', async () => {
    await setup('sub-1', makeWorkspace({ status: 'auto-graded' }));
    await initAndLoad();

    await (component as any).performAutosave();

    expect(submissionsApi.autosaveAnswers).not.toHaveBeenCalled();
  });

  it('onSubmit() không mở modal khi workspace.status != draft', async () => {
    await setup('sub-1', makeWorkspace({ status: 'submitted' }));
    await initAndLoad();

    (component as any).onSubmit();
    fixture.detectChanges();

    expect((component as any).isSubmitConfirmOpen()).toBe(false);
  });
});
