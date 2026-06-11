import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { TestTemplateReviewComponent } from './test-template-review.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';

function makeTemplate(skill: string, status = 'draft') {
  return {
    templateId: 'tpl-1',
    title: 'Test Reading',
    skill,
    description: null,
    tags: [],
    status,
    createdAt: '2026-06-10T00:00:00Z',
    updatedAt: '2026-06-10T00:00:00Z',
    lastUsedAt: null,
    archivedAt: null,
  };
}

function makeAnswerKey() {
  return {
    answerKeyVersionId: 'ak-1',
    templateId: 'tpl-1',
    status: 'draft',
    scoringMode: 'equal' as const,
    questionCount: 2,
    totalScore: 10,
    rows: [
      { questionNumber: 1, correctAnswer: 'A', score: null },
      { questionNumber: 2, correctAnswer: 'B', score: null },
    ],
    updatedAt: '2026-06-11T00:00:00Z',
  };
}

function makeMaterials() {
  return [
    {
      materialId: 'mat-1',
      fileId: 'file-1',
      role: 'pdf' as const,
      originalFileName: 'test.pdf',
      sizeBytes: 1024,
      contentType: 'application/pdf',
      uploadedAt: '2026-06-10T00:00:00Z',
    },
  ];
}

describe('TestTemplateReviewComponent', () => {
  let fixture: ComponentFixture<TestTemplateReviewComponent>;
  let api: {
    getTemplate: ReturnType<typeof vi.fn>;
    listMaterials: ReturnType<typeof vi.fn>;
    getAnswerKey: ReturnType<typeof vi.fn>;
    markReady: ReturnType<typeof vi.fn>;
  };

  async function setup(skill = 'reading', status = 'draft'): Promise<void> {
    api = {
      getTemplate: vi.fn().mockResolvedValue(makeTemplate(skill, status)),
      listMaterials: vi.fn().mockResolvedValue(makeMaterials()),
      getAnswerKey: vi.fn().mockResolvedValue(makeAnswerKey()),
      markReady: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateReviewComponent],
      providers: [
        provideRouter([]),
        { provide: TestTemplatesApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ templateId: 'tpl-1' })),
            snapshot: { paramMap: convertToParamMap({ templateId: 'tpl-1' }) },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestTemplateReviewComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('shows loaded state with template info', async () => {
    await setup();

    expect(document.querySelector('#review-publish-wizard-header')).toBeTruthy();
    expect(document.querySelector('#review-step')?.textContent).toContain('4/4');
    expect(document.querySelector('#review-publish-basic-info-card')).toBeTruthy();
    expect(document.querySelector('#review-publish-material-card')).toBeTruthy();
    expect(document.querySelector('#review-publish-answer-key-card')).toBeTruthy();
    expect(document.querySelector('#review-publish-readiness-panel')).toBeTruthy();
    expect(document.querySelector('#review-publish-button')).toBeTruthy();
  });

  it('shows success state immediately when template is already ready', async () => {
    await setup('reading', 'ready');

    expect(document.querySelector('#review-publish-success-banner')).toBeTruthy();
    expect(document.querySelector('#review-create-homework-button')).toBeTruthy();
    expect(document.querySelector('#review-create-live-exam-button')).toBeTruthy();
    expect(document.querySelector('#review-publish-button')).toBeFalsy();
  });

  it('calls markReady when button clicked and transitions to success', async () => {
    await setup();
    const readyTemplate = { ...makeTemplate('reading', 'ready') };
    api.markReady.mockResolvedValue(readyTemplate);

    const button = document.querySelector<HTMLButtonElement>('#review-publish-button');
    expect(button).toBeTruthy();
    button!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.markReady).toHaveBeenCalledWith('tpl-1');
    expect(document.querySelector('#review-publish-success-banner')).toBeTruthy();
  });

  it('shows error banner and stays on loaded when markReady fails', async () => {
    await setup();
    api.markReady.mockRejectedValue({
      status: 400,
      error: { code: 'review.missingRequiredMaterial' },
    });

    const button = document.querySelector<HTMLButtonElement>('#review-publish-button');
    button!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(document.querySelector('.error-banner')).toBeTruthy();
    expect(document.querySelector('#review-publish-button')).toBeTruthy();
    expect(document.querySelector('#review-publish-success-banner')).toBeFalsy();
  });

  it('does not show answer key card for speaking templates', async () => {
    await setup('speaking');

    expect(document.querySelector('#review-publish-answer-key-card')).toBeFalsy();
    expect(api.getAnswerKey).not.toHaveBeenCalled();
  });

  it('shows readiness checklist with pass/fail items', async () => {
    await setup();

    const items = document.querySelectorAll('.checklist-item');
    expect(items.length).toBeGreaterThan(0);

    const passed = document.querySelectorAll('.checklist-item.passed');
    expect(passed.length).toBeGreaterThan(0);
  });

  it('shows load error when getTemplate fails', async () => {
    api = {
      getTemplate: vi.fn().mockRejectedValue({ status: 404, error: { code: 'templates.notFound' } }),
      listMaterials: vi.fn(),
      getAnswerKey: vi.fn(),
      markReady: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateReviewComponent],
      providers: [
        provideRouter([]),
        { provide: TestTemplatesApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ templateId: 'tpl-1' })),
            snapshot: { paramMap: convertToParamMap({ templateId: 'tpl-1' }) },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestTemplateReviewComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(document.querySelector('.error-banner')).toBeTruthy();
    expect(document.querySelector('#review-publish-button')).toBeFalsy();
  });

  it('navigates to library when no templateId in route', async () => {
    api = {
      getTemplate: vi.fn(),
      listMaterials: vi.fn(),
      getAnswerKey: vi.fn(),
      markReady: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateReviewComponent],
      providers: [
        provideRouter([{ path: 'teacher/library', component: TestTemplateReviewComponent }]),
        { provide: TestTemplatesApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({})),
            snapshot: { paramMap: convertToParamMap({}) },
          },
        },
      ],
    }).compileComponents();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(TestTemplateReviewComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(navigateSpy).toHaveBeenCalledWith(['/teacher/library']);
    expect(api.getTemplate).not.toHaveBeenCalled();
  });
});
