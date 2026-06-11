import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { LiveExamCreateComponent } from './live-exam-create.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { LiveExamApiService } from '../../core/live-exam/live-exam-api.service';

async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeReadyTemplate() {
  return {
    templateId: 'tpl-1',
    title: 'Listening Test',
    skill: 'listening',
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

function makeSession(status = 'scheduled') {
  const allowedActions =
    status === 'scheduled' ? ['open'] : status === 'open' ? ['close'] : [];
  return {
    id: 'ses-1',
    templateId: 'tpl-1',
    templateTitle: 'Listening Test',
    templateSkill: 'listening',
    classId: 'cls-1',
    className: 'Lớp 7A',
    status,
    mode: 'live-exam',
    allowedActions,
    scheduledStartAt: null,
    scheduledEndAt: null,
    openedAt: status === 'open' || status === 'closed' ? '2026-06-11T08:00:00Z' : null,
    closedAt: status === 'closed' ? '2026-06-11T10:00:00Z' : null,
    createdAt: '2026-06-11T00:00:00Z',
  };
}

describe('LiveExamCreateComponent', () => {
  let fixture: ComponentFixture<LiveExamCreateComponent>;
  let component: LiveExamCreateComponent;
  let router: Router;
  let templatesApi: { getTemplate: ReturnType<typeof vi.fn> };
  let classesApi: { getTeacherClasses: ReturnType<typeof vi.fn> };
  let liveExamApi: {
    create: ReturnType<typeof vi.fn>;
    open: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
  };

  async function setup(templateId: string | null = 'tpl-1'): Promise<void> {
    templatesApi = { getTemplate: vi.fn().mockResolvedValue(makeReadyTemplate()) };
    classesApi = { getTeacherClasses: vi.fn().mockResolvedValue(makeClasses()) };
    liveExamApi = {
      create: vi.fn().mockResolvedValue(makeSession('scheduled')),
      open: vi.fn().mockResolvedValue(makeSession('open')),
      close: vi.fn().mockResolvedValue(makeSession('closed')),
    };

    const paramMap = templateId
      ? of(convertToParamMap({ templateId }))
      : of(convertToParamMap({}));

    await TestBed.configureTestingModule({
      imports: [LiveExamCreateComponent],
      providers: [
        provideRouter([]),
        { provide: TestTemplatesApiService, useValue: templatesApi },
        { provide: ClassesApiService, useValue: classesApi },
        { provide: LiveExamApiService, useValue: liveExamApi },
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
    fixture = TestBed.createComponent(LiveExamCreateComponent);
    component = fixture.componentInstance;
  }

  async function initAndLoad(): Promise<void> {
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();
  }

  it('shows loading indicator immediately on init', async () => {
    await setup();
    fixture.detectChanges();
    const loading = fixture.nativeElement.querySelector('[data-testid="loading-indicator"]');
    expect(loading).toBeTruthy();
  });

  it('shows form with template summary and class select after load', async () => {
    await setup();
    await initAndLoad();

    const title = fixture.nativeElement.querySelector('[data-testid="source-template-title"]');
    expect(title).toBeTruthy();
    expect(title.textContent).toContain('Listening Test');

    const classSelect = fixture.nativeElement.querySelector('[data-testid="class-select"]');
    expect(classSelect).toBeTruthy();
  });

  it('shows only active classes in dropdown', async () => {
    await setup();
    await initAndLoad();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('[data-testid="class-select"]');
    expect(select).toBeTruthy();
    const options: HTMLOptionElement[] = Array.from(select.querySelectorAll('option'));
    const classOptions = options.filter((o) => o.value !== '');
    expect(classOptions.length).toBe(1);
    expect(classOptions[0].textContent?.trim()).toContain('Lớp 7A');
  });

  it('create button is disabled when class not selected', async () => {
    await setup();
    await initAndLoad();

    const createBtn: HTMLButtonElement = fixture.nativeElement.querySelector('[data-testid="create-action"]');
    expect(createBtn).toBeTruthy();
    expect(createBtn.disabled).toBe(true);
  });

  it('shows session detail with scheduled badge and open button after create', async () => {
    await setup();
    await initAndLoad();

    (component as any).selectedClassId.set('cls-1');
    fixture.detectChanges();

    await (component as any).onCreate();
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('[data-testid="session-status-badge"]');
    expect(badge).toBeTruthy();
    expect(badge.textContent).toContain('Đã lên lịch');

    const openBtn = fixture.nativeElement.querySelector('[data-testid="open-action"]');
    expect(openBtn).toBeTruthy();

    const closeBtn = fixture.nativeElement.querySelector('[data-testid="close-action"]');
    expect(closeBtn).toBeNull();
  });

  it('after open: session status becomes open, close button visible, open button hidden', async () => {
    await setup();
    await initAndLoad();

    (component as any).selectedClassId.set('cls-1');
    fixture.detectChanges();
    await (component as any).onCreate();
    fixture.detectChanges();

    await (component as any).onOpen();
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('[data-testid="session-status-badge"]');
    expect(badge.textContent).toContain('Đang mở');

    const closeBtn = fixture.nativeElement.querySelector('[data-testid="close-action"]');
    expect(closeBtn).toBeTruthy();

    const openBtn = fixture.nativeElement.querySelector('[data-testid="open-action"]');
    expect(openBtn).toBeNull();
  });

  it('after close: session status becomes closed, no open/close buttons', async () => {
    await setup();
    liveExamApi.create.mockResolvedValue(makeSession('scheduled'));
    liveExamApi.open.mockResolvedValue(makeSession('open'));
    liveExamApi.close.mockResolvedValue(makeSession('closed'));
    await initAndLoad();

    (component as any).selectedClassId.set('cls-1');
    fixture.detectChanges();
    await (component as any).onCreate();
    await (component as any).onOpen();
    await (component as any).onClose();
    fixture.detectChanges();

    const badge = fixture.nativeElement.querySelector('[data-testid="session-status-badge"]');
    expect(badge.textContent).toContain('Đã đóng');

    expect(fixture.nativeElement.querySelector('[data-testid="open-action"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="close-action"]')).toBeNull();
  });

  it('shows api error banner when create fails', async () => {
    await setup();
    liveExamApi.create.mockRejectedValue({
      error: { extensions: { code: 'liveExam.classNotActive' } },
    });
    await initAndLoad();

    (component as any).selectedClassId.set('cls-1');
    fixture.detectChanges();

    await (component as any).onCreate();
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector('[data-testid="api-error"]');
    expect(errorBanner).toBeTruthy();
    expect(errorBanner.textContent).toContain('Lớp học đã không còn hoạt động');
  });

  it('shows api error banner when open fails', async () => {
    await setup();
    liveExamApi.create.mockResolvedValue(makeSession('scheduled'));
    liveExamApi.open.mockRejectedValue({
      error: { extensions: { code: 'liveExam.alreadyOpen' } },
    });
    await initAndLoad();

    (component as any).selectedClassId.set('cls-1');
    fixture.detectChanges();
    await (component as any).onCreate();
    fixture.detectChanges();

    await (component as any).onOpen();
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector('[data-testid="api-error"]');
    expect(errorBanner).toBeTruthy();
    expect(errorBanner.textContent).toContain('Phiên thi đã đang mở');
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

  it('shows load error when template is not ready', async () => {
    await setup();
    templatesApi.getTemplate.mockResolvedValue({ ...makeReadyTemplate(), status: 'draft' });
    await initAndLoad();

    const errorEl = fixture.nativeElement.querySelector('[data-testid="load-error"]');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Sẵn sàng');
  });
});
