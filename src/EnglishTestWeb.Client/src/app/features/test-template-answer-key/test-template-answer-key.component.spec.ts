import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { TestTemplateAnswerKeyComponent } from './test-template-answer-key.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';

function templateDetail(skill: string) {
  return {
    templateId: 'tpl-1',
    title: 'Reading Draft',
    skill,
    description: null,
    tags: [],
    status: 'draft',
    createdAt: '2026-06-10T00:00:00Z',
    updatedAt: '2026-06-10T00:00:00Z',
    lastUsedAt: null,
    archivedAt: null,
  };
}

describe('TestTemplateAnswerKeyComponent', () => {
  let fixture: ComponentFixture<TestTemplateAnswerKeyComponent>;
  let api: {
    getTemplate: ReturnType<typeof vi.fn>;
    getAnswerKey: ReturnType<typeof vi.fn>;
    upsertAnswerKey: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  async function setup(skill = 'reading'): Promise<void> {
    api = {
      getTemplate: vi.fn().mockResolvedValue(templateDetail(skill)),
      getAnswerKey: vi.fn().mockRejectedValue({ status: 404, error: { code: 'answerKey.notFound' } }),
      upsertAnswerKey: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateAnswerKeyComponent],
      providers: [
        provideRouter([
          { path: 'teacher/library/:templateId/review', component: TestTemplateAnswerKeyComponent },
          { path: 'teacher/library/:templateId/materials', component: TestTemplateAnswerKeyComponent },
        ]),
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

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(TestTemplateAnswerKeyComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('initializes default rows when no answer key exists', async () => {
    await setup();
    expect(fixture.componentInstance['rows']().length).toBe(10);
    expect(fixture.componentInstance['scoringMode']()).toBe('equal');
  });

  it('disables continue when answers are missing', async () => {
    await setup();
    expect(fixture.componentInstance['missingAnswerCount']()).toBe(10);
    expect(fixture.componentInstance['canContinue']()).toBe(false);

    const errors = fixture.componentInstance['validateForContinue']();
    expect(errors.some((error) => error.code === 'ERR_ANSWER_MISSING')).toBe(true);
  });

  it('blocks continue with ERR_QUESTION_COUNT_INVALID when count out of range', async () => {
    await setup();
    const input = document.createElement('input');
    input.value = '250';
    fixture.componentInstance['onQuestionCountChange']({ target: input } as unknown as Event);
    fixture.detectChanges();

    expect(fixture.componentInstance['questionCountValid']()).toBe(false);
    expect(fixture.componentInstance['canContinue']()).toBe(false);

    const errors = fixture.componentInstance['validateForContinue']();
    expect(errors[0]?.code).toBe('ERR_QUESTION_COUNT_INVALID');
  });

  it('regenerates rows when question count changes', async () => {
    await setup();
    const input = document.createElement('input');
    input.value = '3';
    fixture.componentInstance['onQuestionCountChange']({ target: input } as unknown as Event);
    fixture.detectChanges();

    const rows = fixture.componentInstance['rows']();
    expect(rows.length).toBe(3);
    expect(rows[2].questionNumber).toBe(3);
  });

  it('toggles total score input with scoring mode', async () => {
    await setup();
    expect(document.querySelector('#answer-key-total-score-input')).toBeTruthy();

    fixture.componentInstance['onScoringModeChange']('per-question');
    fixture.detectChanges();

    expect(document.querySelector('#answer-key-total-score-input')).toBeFalsy();
    expect(document.querySelector('.answer-key-score-input')).toBeTruthy();
  });

  it('updates validation summary when answers change', async () => {
    await setup();
    const input = document.createElement('input');
    input.value = '2';
    fixture.componentInstance['onQuestionCountChange']({ target: input } as unknown as Event);
    fixture.detectChanges();
    expect(fixture.componentInstance['missingAnswerCount']()).toBe(2);

    const answerInput = document.createElement('input');
    answerInput.value = 'A';
    fixture.componentInstance['onAnswerChange'](1, { target: answerInput } as unknown as Event);
    fixture.detectChanges();

    expect(fixture.componentInstance['missingAnswerCount']()).toBe(1);
  });

  it('saves draft without completeness validation', async () => {
    await setup();
    api.upsertAnswerKey.mockResolvedValue({
      answerKeyVersionId: 'ak-1',
      templateId: 'tpl-1',
      status: 'draft',
      scoringMode: 'equal',
      questionCount: 10,
      totalScore: 10,
      rows: [],
      updatedAt: '2026-06-11T00:00:00Z',
    });

    await fixture.componentInstance['onSaveDraft']();
    fixture.detectChanges();

    expect(api.upsertAnswerKey).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance['saveSuccess']()).toBeTruthy();
    expect(fixture.componentInstance['bannerError']()).toBeNull();
  });

  it('shows banner error when save fails', async () => {
    await setup();
    api.upsertAnswerKey.mockRejectedValue({
      status: 409,
      error: { code: 'templates.notEditable' },
    });

    await fixture.componentInstance['onSaveDraft']();
    fixture.detectChanges();

    expect(fixture.componentInstance['bannerError']()).toBe(
      'Chỉ có thể chỉnh sửa đề ở trạng thái Nháp.',
    );
  });

  it('navigates to review after continue with valid data', async () => {
    await setup();
    api.upsertAnswerKey.mockResolvedValue({
      answerKeyVersionId: 'ak-1',
      templateId: 'tpl-1',
      status: 'draft',
      scoringMode: 'equal',
      questionCount: 2,
      totalScore: 10,
      rows: [
        { questionNumber: 1, correctAnswer: 'A', score: null },
        { questionNumber: 2, correctAnswer: 'B', score: null },
      ],
      updatedAt: '2026-06-11T00:00:00Z',
    });
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const countInput = document.createElement('input');
    countInput.value = '2';
    fixture.componentInstance['onQuestionCountChange']({ target: countInput } as unknown as Event);
    for (const questionNumber of [1, 2]) {
      const answerInput = document.createElement('input');
      answerInput.value = 'A';
      fixture.componentInstance['onAnswerChange'](questionNumber, {
        target: answerInput,
      } as unknown as Event);
    }
    fixture.detectChanges();

    await fixture.componentInstance['onContinue']();

    expect(navigateSpy).toHaveBeenCalledWith(['/teacher/library', 'tpl-1', 'review']);
  });

  it('shows not-applicable state for speaking templates', async () => {
    await setup('speaking');

    expect(document.querySelector('#answer-key-not-applicable')).toBeTruthy();
    expect(document.querySelector('#answer-key-grid')).toBeFalsy();
    expect(api.getAnswerKey).not.toHaveBeenCalled();
  });

  it('loads existing answer key into the grid', async () => {
    api = {
      getTemplate: vi.fn().mockResolvedValue(templateDetail('reading')),
      getAnswerKey: vi.fn().mockResolvedValue({
        answerKeyVersionId: 'ak-1',
        templateId: 'tpl-1',
        status: 'draft',
        scoringMode: 'per-question',
        questionCount: 2,
        totalScore: null,
        rows: [
          { questionNumber: 1, correctAnswer: 'A', score: 2 },
          { questionNumber: 2, correctAnswer: 'B', score: 3 },
        ],
        updatedAt: '2026-06-11T00:00:00Z',
      }),
      upsertAnswerKey: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateAnswerKeyComponent],
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

    fixture = TestBed.createComponent(TestTemplateAnswerKeyComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component['questionCount']()).toBe(2);
    expect(component['scoringMode']()).toBe('per-question');
    expect(component['rows']()[0].correctAnswer).toBe('A');
    expect(component['scoreTotal']()).toBe(5);
    expect(component['missingAnswerCount']()).toBe(0);
  });

  it('blocks continue when total score is zero in equal mode', async () => {
    await setup();
    fixture.componentInstance['scoringMode'].set('equal');
    fixture.componentInstance['totalScore'].set(0);
    fixture.detectChanges();

    expect(fixture.componentInstance['canContinue']()).toBe(false);

    const errors = fixture.componentInstance['validateForContinue']();
    expect(errors.some((e) => e.code === 'ERR_TOTAL_SCORE_INVALID')).toBe(true);
  });

  it('blocks continue when per-question scores are missing', async () => {
    await setup();
    const countInput = document.createElement('input');
    countInput.value = '2';
    fixture.componentInstance['onQuestionCountChange']({ target: countInput } as unknown as Event);
    fixture.componentInstance['onScoringModeChange']('per-question');

    for (const questionNumber of [1, 2]) {
      const answerInput = document.createElement('input');
      answerInput.value = 'A';
      fixture.componentInstance['onAnswerChange'](questionNumber, {
        target: answerInput,
      } as unknown as Event);
    }
    fixture.detectChanges();

    expect(fixture.componentInstance['canContinue']()).toBe(false);

    const errors = fixture.componentInstance['validateForContinue']();
    expect(errors.some((e) => e.code === 'ERR_ROW_SCORE_INVALID')).toBe(true);
  });
});
