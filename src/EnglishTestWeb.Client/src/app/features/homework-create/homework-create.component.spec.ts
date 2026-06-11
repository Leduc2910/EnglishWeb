import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { HomeworkCreateComponent } from './homework-create.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { HomeworkApiService } from '../../core/homework/homework-api.service';

// Flush all pending microtasks (needed for void promise chains in components)
async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeReadyTemplate() {
  return {
    templateId: 'tpl-1',
    title: 'Reading Test',
    skill: 'reading',
    description: null,
    tags: [],
    status: 'ready',
    createdAt: '2026-06-10T00:00:00Z',
    updatedAt: '2026-06-10T00:00:00Z',
    lastUsedAt: null,
    archivedAt: null,
  };
}

function makeClasses() {
  return [
    { classId: 'cls-1', className: 'Lớp 7A', classCode: 'ENG7A', status: 'active', enrolledStudentCount: 25 },
    { classId: 'cls-2', className: 'Lớp 8B', classCode: 'ENG8B', status: 'inactive', enrolledStudentCount: 20 },
  ];
}

function makeAssignment() {
  return {
    id: 'hw-1',
    templateId: 'tpl-1',
    templateTitle: 'Reading Test',
    templateSkill: 'reading',
    classId: 'cls-1',
    className: 'Lớp 7A',
    deadlineAt: '2026-12-31T12:00:00Z',
    timeLimitMinutes: null,
    status: 'published',
    createdAt: '2026-06-11T00:00:00Z',
  };
}

describe('HomeworkCreateComponent', () => {
  let fixture: ComponentFixture<HomeworkCreateComponent>;
  let component: HomeworkCreateComponent;
  let router: Router;
  let templatesApi: { getTemplate: ReturnType<typeof vi.fn> };
  let classesApi: { getTeacherClasses: ReturnType<typeof vi.fn> };
  let homeworkApi: { create: ReturnType<typeof vi.fn> };

  async function setup(templateId: string | null = 'tpl-1'): Promise<void> {
    templatesApi = { getTemplate: vi.fn().mockResolvedValue(makeReadyTemplate()) };
    classesApi = { getTeacherClasses: vi.fn().mockResolvedValue(makeClasses()) };
    homeworkApi = { create: vi.fn().mockResolvedValue(makeAssignment()) };

    const paramMap = templateId
      ? of(convertToParamMap({ templateId }))
      : of(convertToParamMap({}));

    await TestBed.configureTestingModule({
      imports: [HomeworkCreateComponent],
      providers: [
        provideRouter([]),
        { provide: TestTemplatesApiService, useValue: templatesApi },
        { provide: ClassesApiService, useValue: classesApi },
        { provide: HomeworkApiService, useValue: homeworkApi },
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: paramMap,
            snapshot: {
              queryParamMap: convertToParamMap(templateId ? { templateId } : {}),
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(HomeworkCreateComponent);
    component = fixture.componentInstance;
  }

  async function initAndLoad(): Promise<void> {
    fixture.detectChanges(); // triggers ngOnInit
    await flushPromises();   // flush Promise.all resolution
    fixture.detectChanges(); // render updated state
  }

  it('shows loading indicator immediately on init', async () => {
    await setup();
    fixture.detectChanges();
    const loading = fixture.nativeElement.querySelector('#homework-create-loading');
    expect(loading).toBeTruthy();
  });

  it('shows form with template summary and class dropdown after load', async () => {
    await setup();
    await initAndLoad();

    const form = fixture.nativeElement.querySelector('form');
    expect(form).toBeTruthy();

    const sourceTemplate = fixture.nativeElement.querySelector('#homework-create-source-template');
    expect(sourceTemplate).toBeTruthy();
    expect(sourceTemplate.textContent).toContain('Reading Test');

    const classSelect = fixture.nativeElement.querySelector('#homework-create-class-select');
    expect(classSelect).toBeTruthy();
  });

  it('shows only active classes in dropdown', async () => {
    await setup();
    await initAndLoad();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#homework-create-class-select');
    expect(select).toBeTruthy();

    const options: HTMLOptionElement[] = Array.from(select.querySelectorAll('option'));
    const classOptions = options.filter((o) => o.value !== '');
    // Only the 'active' class should be rendered
    expect(classOptions.length).toBe(1);
    expect(classOptions[0].textContent?.trim()).toContain('Lớp 7A');
  });

  it('submit button is disabled when class or deadline not selected', async () => {
    await setup();
    await initAndLoad();

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('#homework-create-submit');
    expect(submitBtn).toBeTruthy();
    // Nothing selected yet — form is invalid
    expect(submitBtn.disabled).toBe(true);
  });

  it('shows success banner after successful create', async () => {
    await setup();
    await initAndLoad();

    // Populate required fields via signal setters
    (component as any).selectedClassId.set('cls-1');
    (component as any).deadlineAt.set('2026-12-31T12:00');
    fixture.detectChanges();

    await (component as any).onSubmit();
    fixture.detectChanges();

    const success = fixture.nativeElement.querySelector('#homework-create-success');
    expect(success).toBeTruthy();
    expect(success.textContent).toContain('Lớp 7A');
  });

  it('shows API error banner when create fails', async () => {
    await setup();
    homeworkApi.create.mockRejectedValue({
      error: { extensions: { code: 'homework.deadlinePast' } },
    });
    await initAndLoad();

    (component as any).selectedClassId.set('cls-1');
    (component as any).deadlineAt.set('2026-12-31T12:00');
    fixture.detectChanges();

    await (component as any).onSubmit();
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector('#homework-create-validation-error');
    expect(errorBanner).toBeTruthy();
    expect(errorBanner.textContent).toContain('Hạn nộp phải là thời điểm trong tương lai.');
  });

  it('cancel navigates back to library review', async () => {
    await setup();
    await initAndLoad();

    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    (component as any).onCancel();

    expect(navigateSpy).toHaveBeenCalledWith(['/teacher/library', 'tpl-1', 'review']);
  });

  it('redirects to library when no templateId in query params', async () => {
    await setup(null);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.detectChanges();
    await flushPromises();

    expect(navigateSpy).toHaveBeenCalledWith(['/teacher/library']);
  });

  it('shows load error when template status is not ready', async () => {
    await setup();
    templatesApi.getTemplate.mockResolvedValue({ ...makeReadyTemplate(), status: 'draft' });
    await initAndLoad();

    const errorEl = fixture.nativeElement.querySelector('#homework-create-error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Sẵn sàng');
  });
});
