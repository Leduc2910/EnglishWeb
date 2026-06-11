import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { TestTemplateSetupComponent } from './test-template-setup.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { TEMPLATE_ERROR_MESSAGES } from '../../core/test-templates/test-templates.models';

describe('TestTemplateSetupComponent', () => {
  let fixture: ComponentFixture<TestTemplateSetupComponent>;
  let api: {
    getTemplate: ReturnType<typeof vi.fn>;
    createTemplate: ReturnType<typeof vi.fn>;
    updateTemplate: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    api = {
      getTemplate: vi.fn(),
      createTemplate: vi.fn().mockResolvedValue({
        templateId: 'tpl-new',
        title: 'New Draft',
        skill: 'reading',
        description: null,
        tags: [],
        status: 'draft',
        createdAt: '2026-06-10T00:00:00Z',
        updatedAt: '2026-06-10T00:00:00Z',
        lastUsedAt: null,
        archivedAt: null,
      }),
      updateTemplate: vi.fn().mockResolvedValue({
        templateId: 'tpl-new',
        title: 'New Draft',
        skill: 'reading',
        description: null,
        tags: [],
        status: 'draft',
        createdAt: '2026-06-10T00:00:00Z',
        updatedAt: '2026-06-10T00:00:00Z',
        lastUsedAt: null,
        archivedAt: null,
      }),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateSetupComponent],
      providers: [
        provideRouter([
          { path: 'teacher/library/new/setup', component: TestTemplateSetupComponent },
          {
            path: 'teacher/library/:templateId/setup',
            component: TestTemplateSetupComponent,
          },
          {
            path: 'teacher/library/:templateId/materials',
            component: TestTemplateSetupComponent,
          },
          { path: 'teacher/library', component: TestTemplateSetupComponent },
        ]),
        { provide: TestTemplatesApiService, useValue: api },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
  });

  it('shows validation error for short title', async () => {
    await router.navigateByUrl('/teacher/library/new/setup');
    fixture = TestBed.createComponent(TestTemplateSetupComponent);
    fixture.detectChanges();

    fixture.componentInstance['form'].patchValue({ title: 'ab', skill: 'reading' });
    fixture.componentInstance['form'].markAllAsTouched();
    fixture.detectChanges();

    expect(fixture.componentInstance['titleError']()).toBe(
      TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NAME_REQUIRED'],
    );
  });

  it('updates sidebar checklist when form skill changes', async () => {
    await router.navigateByUrl('/teacher/library/new/setup');
    fixture = TestBed.createComponent(TestTemplateSetupComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance['checklist']().some((item) => item.label.includes('Answer key'))).toBe(
      true,
    );

    fixture.componentInstance['form'].patchValue({ skill: 'listening' });
    fixture.detectChanges();

    expect(fixture.componentInstance['checklist']().some((item) => item.label.includes('Audio'))).toBe(
      true,
    );
  });

  it('creates draft on first save then uses PUT on second save', async () => {
    await router.navigateByUrl('/teacher/library/new/setup');
    fixture = TestBed.createComponent(TestTemplateSetupComponent);
    fixture.detectChanges();

    fixture.componentInstance['form'].patchValue({
      title: 'New Draft',
      skill: 'reading',
      description: '',
      tagsInput: '',
    });

    await fixture.componentInstance['onSaveDraft']();
    expect(api.createTemplate).toHaveBeenCalledTimes(1);
    expect(api.updateTemplate).not.toHaveBeenCalled();

    await fixture.componentInstance['onSaveDraft']();
    expect(api.updateTemplate).toHaveBeenCalledTimes(1);
  });

  it('uses PUT not second POST when continuing after first save', async () => {
    await router.navigateByUrl('/teacher/library/new/setup');
    fixture = TestBed.createComponent(TestTemplateSetupComponent);
    fixture.detectChanges();

    fixture.componentInstance['form'].patchValue({
      title: 'New Draft',
      skill: 'reading',
      description: '',
      tagsInput: '',
    });

    await fixture.componentInstance['onSaveDraft']();
    api.createTemplate.mockClear();
    api.updateTemplate.mockClear();

    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    await fixture.componentInstance['onContinue']();

    expect(api.createTemplate).not.toHaveBeenCalled();
    expect(api.updateTemplate).toHaveBeenCalledTimes(1);
    navigateSpy.mockRestore();
  });
});

describe('TestTemplateSetupComponent edit mode', () => {
  let fixture: ComponentFixture<TestTemplateSetupComponent>;
  const api = {
    getTemplate: vi.fn().mockResolvedValue({
      templateId: 'tpl-existing',
      title: 'Existing Draft',
      skill: 'reading',
      description: 'Saved note',
      tags: ['midterm'],
      status: 'draft',
      createdAt: '2026-06-01T00:00:00Z',
      updatedAt: '2026-06-05T00:00:00Z',
      lastUsedAt: null,
      archivedAt: null,
    }),
    createTemplate: vi.fn(),
    updateTemplate: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestTemplateSetupComponent],
      providers: [
        provideRouter([]),
        { provide: TestTemplatesApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ templateId: 'tpl-existing' })),
            snapshot: {
              paramMap: convertToParamMap({ templateId: 'tpl-existing' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestTemplateSetupComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads existing draft values in edit mode', () => {
    expect(api.getTemplate).toHaveBeenCalledWith('tpl-existing');
    expect(fixture.componentInstance['form'].getRawValue().title).toBe('Existing Draft');
    expect(fixture.componentInstance['form'].getRawValue().tagsInput).toBe('midterm');
  });
});

describe('TestTemplateSetupComponent non-draft edit mode', () => {
  let fixture: ComponentFixture<TestTemplateSetupComponent>;
  const api = {
    getTemplate: vi.fn().mockResolvedValue({
      templateId: 'tpl-ready',
      title: 'Ready Template',
      skill: 'reading',
      description: null,
      tags: [],
      status: 'ready',
      createdAt: '2026-06-01T00:00:00Z',
      updatedAt: '2026-06-05T00:00:00Z',
      lastUsedAt: null,
      archivedAt: null,
    }),
    createTemplate: vi.fn(),
    updateTemplate: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestTemplateSetupComponent],
      providers: [
        provideRouter([]),
        { provide: TestTemplatesApiService, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ templateId: 'tpl-ready' })),
            snapshot: {
              paramMap: convertToParamMap({ templateId: 'tpl-ready' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestTemplateSetupComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('blocks setup editing when template is not draft', () => {
    expect(fixture.componentInstance['loadError']()).toBe(
      TEMPLATE_ERROR_MESSAGES['templates.notEditable'],
    );
  });
});
