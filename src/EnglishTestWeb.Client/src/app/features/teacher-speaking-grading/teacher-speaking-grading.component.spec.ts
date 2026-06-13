import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { TeacherSpeakingGradingComponent } from './teacher-speaking-grading.component';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import { TeacherSpeakingSubmissionDto } from '../../core/speaking/speaking.models';

async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeDto(overrides: Partial<TeacherSpeakingSubmissionDto> = {}): TeacherSpeakingSubmissionDto {
  return {
    id: 'spk-1',
    studentName: 'Nguyễn Văn A',
    className: 'Lớp 7A',
    templateTitle: 'Unit 3 Speaking',
    mode: 'homework',
    status: 'submitted',
    submittedAt: '2026-06-13T09:00:00Z',
    submittedFileName: 'recording.webm',
    submittedFileSizeBytes: 2048,
    submittedFileId: 'file-1',
    isFileMissing: false,
    score: null,
    feedback: null,
    graderId: null,
    gradedAt: null,
    ...overrides,
  };
}

describe('TeacherSpeakingGradingComponent', () => {
  let fixture: ComponentFixture<TeacherSpeakingGradingComponent>;
  let speakingApi: {
    getForTeacher: ReturnType<typeof vi.fn>;
    grade: ReturnType<typeof vi.fn>;
    getTeacherSubmissionFileUrl: ReturnType<typeof vi.fn>;
  };

  async function setup(
    speakingSubmissionId: string | null,
    dto: TeacherSpeakingSubmissionDto | null = makeDto(),
  ): Promise<void> {
    speakingApi = {
      getForTeacher: dto
        ? vi.fn().mockResolvedValue(dto)
        : vi.fn().mockRejectedValue({ error: { extensions: { code: 'speaking.notFound' } } }),
      grade: vi.fn().mockResolvedValue(makeDto({ status: 'graded', score: 8, gradedAt: '2026-06-13T10:00:00Z' })),
      getTeacherSubmissionFileUrl: vi.fn().mockReturnValue('/api/teacher/speaking-submissions/spk-1/file'),
    };

    await TestBed.configureTestingModule({
      imports: [TeacherSpeakingGradingComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(
                speakingSubmissionId ? { speakingSubmissionId } : {},
              ),
            },
          },
        },
        { provide: SpeakingApiService, useValue: speakingApi },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TeacherSpeakingGradingComponent);
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();
  }

  it('shows loading then renders submission info', async () => {
    await setup('spk-1');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="template-title"]')?.textContent?.trim()).toBe(
      'Unit 3 Speaking',
    );
    expect(el.querySelector('[data-testid="student-name"]')?.textContent?.trim()).toBe(
      'Nguyễn Văn A',
    );
    expect(el.querySelector('[data-testid="status-badge"]')?.textContent?.trim()).toBe('Đã nộp');
  });

  it('shows audio player when file is present', async () => {
    await setup('spk-1');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="audio-player"]')).toBeTruthy();
    expect(el.querySelector('[data-testid="audio-element"]')).toBeTruthy();
  });

  it('shows grading form for submitted status', async () => {
    await setup('spk-1');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="grade-form"]')).toBeTruthy();
    expect(el.querySelector('[data-testid="score-input"]')).toBeTruthy();
  });

  it('shows "cannot grade" for draft status', async () => {
    await setup('spk-1', makeDto({ status: 'draft' }));
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="cannot-grade"]')).toBeTruthy();
    expect(el.querySelector('[data-testid="grade-form"]')).toBeFalsy();
  });

  it('shows existing grade info for graded status', async () => {
    await setup(
      'spk-1',
      makeDto({ status: 'graded', score: 9, feedback: 'Very good', gradedAt: '2026-06-13T10:00:00Z' }),
    );
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="current-score"]')?.textContent?.trim()).toBe('9');
    expect(el.querySelector('[data-testid="grade-form"]')).toBeTruthy();
  });

  it('shows error state when load fails', async () => {
    await setup('spk-1', null);
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="load-error"]')).toBeTruthy();
  });

  it('calls grade API and shows success on submit', async () => {
    await setup('spk-1');
    const el = fixture.nativeElement as HTMLElement;
    const scoreInput = el.querySelector<HTMLInputElement>('[data-testid="score-input"]')!;
    scoreInput.value = '8';
    scoreInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const submitBtn = el.querySelector<HTMLButtonElement>('[data-testid="grade-submit-btn"]')!;
    submitBtn.click();
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();

    expect(speakingApi.grade).toHaveBeenCalledWith('spk-1', { score: 8, feedback: null });
    expect(el.querySelector('[data-testid="grade-success"]')).toBeTruthy();
  });

  it('status-submitted badge uses status-submitted CSS class (blue, not amber)', async () => {
    await setup('spk-1', makeDto({ status: 'submitted' }));
    const el = fixture.nativeElement as HTMLElement;
    const badge = el.querySelector('[data-testid="status-badge"]');
    expect(badge).toBeTruthy();
    expect(badge?.classList.contains('status-submitted')).toBe(true);
  });
});
