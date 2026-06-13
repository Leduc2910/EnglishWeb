import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { convertToParamMap } from '@angular/router';
import { StudentSpeakingSubmissionComponent } from './student-speaking-submission.component';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import { SpeakingSubmissionDto } from '../../core/speaking/speaking.models';

async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeDraftFile() {
  return {
    fileId: 'file-1',
    originalFileName: 'recording.webm',
    sizeBytes: 2048,
    uploadedAt: '2026-06-13T09:00:00Z',
  };
}

function makeDto(overrides: Partial<SpeakingSubmissionDto> = {}): SpeakingSubmissionDto {
  return {
    id: 'spk-1',
    status: 'draft',
    mode: 'homework',
    templateTitle: 'Unit 3 Speaking',
    templateSkill: 'speaking',
    className: 'Lớp 7A',
    isSourceOpen: true,
    cueMaterialFileId: null,
    cueMaterialFileName: null,
    draftFile: null,
    submittedAt: null,
    ...overrides,
  };
}

describe('StudentSpeakingSubmissionComponent', () => {
  let fixture: ComponentFixture<StudentSpeakingSubmissionComponent>;
  let component: StudentSpeakingSubmissionComponent;
  let speakingApi: {
    get: ReturnType<typeof vi.fn>;
    uploadDraft: ReturnType<typeof vi.fn>;
    createOrResume: ReturnType<typeof vi.fn>;
    finalSubmit: ReturnType<typeof vi.fn>;
  };

  async function setup(
    speakingSubmissionId: string | null,
    dto: SpeakingSubmissionDto | null = makeDto(),
  ): Promise<void> {
    const submittedDto = makeDto({ status: 'submitted', submittedAt: '2026-06-13T10:00:00Z', draftFile: makeDraftFile() });
    speakingApi = {
      get: dto
        ? vi.fn().mockResolvedValue(dto)
        : vi.fn().mockRejectedValue({ error: { extensions: { code: 'speaking.notFound' } } }),
      uploadDraft: vi.fn().mockResolvedValue(dto ?? makeDto()),
      createOrResume: vi.fn().mockResolvedValue(dto ?? makeDto()),
      finalSubmit: vi.fn().mockResolvedValue(submittedDto),
    };

    await TestBed.configureTestingModule({
      imports: [StudentSpeakingSubmissionComponent],
      providers: [
        provideRouter([]),
        { provide: SpeakingApiService, useValue: speakingApi },
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
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentSpeakingSubmissionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();
  }

  it('shows loading state initially', async () => {
    speakingApi = {
      get: vi.fn().mockReturnValue(new Promise(() => {})), // never resolves
      uploadDraft: vi.fn(),
      createOrResume: vi.fn(),
      finalSubmit: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StudentSpeakingSubmissionComponent],
      providers: [
        provideRouter([]),
        { provide: SpeakingApiService, useValue: speakingApi },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ speakingSubmissionId: 'spk-1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentSpeakingSubmissionComponent);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Đang tải');
  });

  it('renders template title after load', async () => {
    await setup('spk-1', makeDto({ templateTitle: 'Unit 3 Speaking' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="template-title"]')?.textContent?.trim()).toBe(
      'Unit 3 Speaking',
    );
  });

  it('shows mode badge for homework', async () => {
    await setup('spk-1', makeDto({ mode: 'homework' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="mode-badge"]')?.textContent?.trim()).toContain('Bài tập');
  });

  it('shows source open status', async () => {
    await setup('spk-1', makeDto({ isSourceOpen: true }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="source-status"]')?.textContent?.trim()).toBe('Đang mở');
  });

  it('shows source closed status', async () => {
    await setup('spk-1', makeDto({ isSourceOpen: false }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="source-status"]')?.textContent?.trim()).toBe('Đã đóng');
  });

  it('shows no draft message when draftFile is null', async () => {
    await setup('spk-1', makeDto({ draftFile: null }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="no-draft"]')).not.toBeNull();
  });

  it('shows draft file info when draftFile is set', async () => {
    await setup(
      'spk-1',
      makeDto({
        draftFile: {
          fileId: 'file-1',
          originalFileName: 'recording.webm',
          sizeBytes: 1048576,
          uploadedAt: '2026-06-12T10:00:00Z',
        },
      }),
    );
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="draft-filename"]')?.textContent?.trim()).toBe(
      'recording.webm',
    );
  });

  it('shows upload section when source is open and status is draft', async () => {
    await setup('spk-1', makeDto({ isSourceOpen: true, status: 'draft' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="upload-section"]')).not.toBeNull();
    expect(el.querySelector('[data-testid="file-input"]')).not.toBeNull();
  });

  it('hides upload section when source is closed', async () => {
    await setup('spk-1', makeDto({ isSourceOpen: false, status: 'draft' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="upload-section"]')).toBeNull();
    expect(el.querySelector('[data-testid="closed-notice"]')).not.toBeNull();
  });

  it('hides upload section when already submitted', async () => {
    await setup('spk-1', makeDto({ status: 'submitted', isSourceOpen: true, draftFile: makeDraftFile(), submittedAt: '2026-06-13T10:00:00Z' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="upload-section"]')).toBeNull();
    expect(el.querySelector('[data-testid="success-panel"]')).not.toBeNull();
  });

  it('does not show closed-notice when already submitted', async () => {
    await setup('spk-1', makeDto({ status: 'submitted', isSourceOpen: false, draftFile: makeDraftFile(), submittedAt: '2026-06-13T10:00:00Z' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="closed-notice"]')).toBeNull();
    expect(el.querySelector('[data-testid="success-panel"]')).not.toBeNull();
  });

  it('shows cue material filename when present', async () => {
    await setup(
      'spk-1',
      makeDto({ cueMaterialFileId: 'file-cue', cueMaterialFileName: 'cue.pdf' }),
    );
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="cue-filename"]')?.textContent?.trim()).toBe('cue.pdf');
  });

  it('shows no-cue message when cueMaterialFileName is null', async () => {
    await setup('spk-1', makeDto({ cueMaterialFileName: null }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="no-cue"]')).not.toBeNull();
  });

  it('shows error state when load fails', async () => {
    await setup('spk-1', null);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="load-error"]')).not.toBeNull();
    expect(el.querySelector('[data-testid="load-error"]')?.textContent).toContain('Không tìm thấy');
  });

  it('shows client validation error for unsupported mime type', async () => {
    await setup('spk-1');
    const file = new File(['data'], 'bad.pdf', { type: 'application/pdf' });
    const fakeEvent = { target: { files: [file] } } as unknown as Event;
    component['onFileSelected'](fakeEvent);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(
      el.querySelector('[data-testid="client-validation-error"]')?.textContent,
    ).toContain('không được hỗ trợ');
  });

  it('calls uploadDraft and refreshes dto on upload', async () => {
    const updatedDto = makeDto({
      draftFile: {
        fileId: 'file-new',
        originalFileName: 'new.webm',
        sizeBytes: 2048,
        uploadedAt: '2026-06-12T11:00:00Z',
      },
    });
    await setup('spk-1');
    speakingApi.uploadDraft.mockResolvedValue(updatedDto);

    const file = new File([new Uint8Array(512)], 'new.webm', { type: 'audio/webm' });
    const fakeEvent = { target: { files: [file] } } as unknown as Event;
    component['onFileSelected'](fakeEvent);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const uploadBtn = el.querySelector<HTMLButtonElement>('[data-testid="upload-button"]');
    uploadBtn?.click();
    await flushPromises();
    fixture.detectChanges();

    expect(speakingApi.uploadDraft).toHaveBeenCalledWith('spk-1', file);
    expect(el.querySelector('[data-testid="draft-filename"]')?.textContent?.trim()).toBe('new.webm');
  });

  // ---- Final Submit ----

  it('shows final-submit-btn when status=draft', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="final-submit-btn"]')).not.toBeNull();
  });

  it('disables final-submit-btn when no draftFile', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: null }));
    const el: HTMLElement = fixture.nativeElement;
    const btn = el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]');
    expect(btn?.disabled).toBe(true);
  });

  it('shows no-file-hint when draftFile is null and status=draft', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: null }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="no-file-hint"]')).not.toBeNull();
  });

  it('disables final-submit-btn when upload is in progress', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile() }));
    // Simulate upload in progress
    component['uploadState'].set('uploading');
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    const btn = el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]');
    expect(btn?.disabled).toBe(true);
  });

  it('disables final-submit-btn when source is closed', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: false, draftFile: makeDraftFile() }));
    const el: HTMLElement = fixture.nativeElement;
    const btn = el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]');
    expect(btn?.disabled).toBe(true);
  });

  it('hides final-submit-btn when status=submitted', async () => {
    await setup('spk-1', makeDto({ status: 'submitted', draftFile: makeDraftFile(), submittedAt: '2026-06-13T10:00:00Z' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="final-submit-btn"]')).toBeNull();
  });

  it('clicking final-submit-btn opens confirm-modal', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile() }));
    const el: HTMLElement = fixture.nativeElement;
    const btn = el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]');
    btn?.click();
    fixture.detectChanges();
    expect(el.querySelector('[data-testid="confirm-modal"]')).not.toBeNull();
  });

  it('confirm-modal shows filename, templateTitle, className, mode', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile(), templateTitle: 'Test Title' }));
    const el: HTMLElement = fixture.nativeElement;
    el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]')?.click();
    fixture.detectChanges();
    expect(el.querySelector('[data-testid="modal-filename"]')?.textContent?.trim()).toBe('recording.webm');
    expect(el.querySelector('[data-testid="modal-template-title"]')?.textContent?.trim()).toBe('Test Title');
    expect(el.querySelector('[data-testid="modal-class"]')?.textContent?.trim()).toBe('Lớp 7A');
    expect(el.querySelector('[data-testid="modal-mode"]')?.textContent).toContain('Bài tập');
  });

  it('clicking cancel-submit-btn closes confirm-modal', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile() }));
    const el: HTMLElement = fixture.nativeElement;
    el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]')?.click();
    fixture.detectChanges();
    el.querySelector<HTMLButtonElement>('[data-testid="cancel-submit-btn"]')?.click();
    fixture.detectChanges();
    expect(el.querySelector('[data-testid="confirm-modal"]')).toBeNull();
  });

  it('clicking confirm-submit-btn calls finalSubmit and shows success panel', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile() }));
    const el: HTMLElement = fixture.nativeElement;
    el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]')?.click();
    fixture.detectChanges();
    el.querySelector<HTMLButtonElement>('[data-testid="confirm-submit-btn"]')?.click();
    await flushPromises();
    fixture.detectChanges();

    expect(speakingApi.finalSubmit).toHaveBeenCalledWith('spk-1');
    expect(el.querySelector('[data-testid="success-panel"]')).not.toBeNull();
  });

  it('success panel shows filename, submittedAt, className, mode', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile() }));
    const el: HTMLElement = fixture.nativeElement;
    el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]')?.click();
    fixture.detectChanges();
    el.querySelector<HTMLButtonElement>('[data-testid="confirm-submit-btn"]')?.click();
    await flushPromises();
    fixture.detectChanges();

    expect(el.querySelector('[data-testid="success-filename"]')?.textContent?.trim()).toBe('recording.webm');
    const submittedAtText = el.querySelector('[data-testid="success-submitted-at"]')?.textContent?.trim() ?? '';
    expect(submittedAtText).not.toBe('—');
    expect(submittedAtText.length).toBeGreaterThan(0);
    expect(el.querySelector('[data-testid="success-class"]')?.textContent?.trim()).toBe('Lớp 7A');
    expect(el.querySelector('[data-testid="success-mode"]')?.textContent).toContain('Bài tập');
    expect(el.querySelector('[data-testid="back-to-tests-btn"]')).not.toBeNull();
  });

  it('shows success panel when dto loaded with status=submitted', async () => {
    await setup('spk-1', makeDto({ status: 'submitted', draftFile: makeDraftFile(), submittedAt: '2026-06-13T10:00:00Z' }));
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="success-panel"]')).not.toBeNull();
  });

  it('shows submit-error when finalSubmit fails', async () => {
    await setup('spk-1', makeDto({ status: 'draft', isSourceOpen: true, draftFile: makeDraftFile() }));
    speakingApi.finalSubmit.mockRejectedValue({ error: { extensions: { code: 'speaking.sourceUnavailable' } } });

    const el: HTMLElement = fixture.nativeElement;
    el.querySelector<HTMLButtonElement>('[data-testid="final-submit-btn"]')?.click();
    fixture.detectChanges();
    el.querySelector<HTMLButtonElement>('[data-testid="confirm-submit-btn"]')?.click();
    await flushPromises();
    fixture.detectChanges();

    const submitError = el.querySelector('[data-testid="submit-error"]');
    expect(submitError).not.toBeNull();
    expect(submitError?.textContent).toContain('khả dụng');
  });
});
