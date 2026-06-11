import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TestTemplateLibraryComponent } from './test-template-library.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { TEMPLATE_ERROR_MESSAGES, TestTemplateListItem } from '../../core/test-templates/test-templates.models';

const draftTemplate: TestTemplateListItem = {
  templateId: 'tpl-draft',
  title: 'Reading Draft',
  skill: 'reading',
  status: 'draft',
  lastUsedAt: null,
  updatedAt: '2026-06-01T00:00:00Z',
};

const readyTemplate: TestTemplateListItem = {
  templateId: 'tpl-ready',
  title: 'Listening Ready',
  skill: 'listening',
  status: 'ready',
  lastUsedAt: '2026-06-05T00:00:00Z',
  updatedAt: '2026-06-06T00:00:00Z',
};

describe('TestTemplateLibraryComponent', () => {
  let fixture: ComponentFixture<TestTemplateLibraryComponent>;
  let api: {
    listTemplates: ReturnType<typeof vi.fn>;
    getTemplate: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    api = {
      listTemplates: vi.fn().mockResolvedValue([draftTemplate, readyTemplate]),
      getTemplate: vi.fn().mockResolvedValue({
        templateId: 'tpl-ready',
        title: 'Listening Ready',
        skill: 'listening',
        description: 'Demo template',
        tags: [],
        status: 'ready',
        createdAt: '2026-06-01T00:00:00Z',
        updatedAt: '2026-06-06T00:00:00Z',
        lastUsedAt: '2026-06-05T00:00:00Z',
        archivedAt: null,
      }),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateLibraryComponent],
      providers: [
        provideRouter([
          {
            path: 'teacher/library',
            component: TestTemplateLibraryComponent,
          },
          {
            path: 'teacher/library/:templateId/setup',
            component: TestTemplateLibraryComponent,
          },
        ]),
        { provide: TestTemplatesApiService, useValue: api },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    await router.navigateByUrl('/teacher/library');
    fixture = TestBed.createComponent(TestTemplateLibraryComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('renders template list with skill and status labels', async () => {
    await fixture.componentInstance['loadTemplates']();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Reading Draft');
    expect(element.textContent).toContain('Listening Ready');
    expect(element.textContent).toContain('Nháp');
    expect(element.textContent).toContain('Sẵn sàng sử dụng');
  });

  it('shows empty state when no templates match filters', async () => {
    api.listTemplates.mockResolvedValueOnce([]);
    fixture.componentInstance['filters'].set({ skill: 'speaking', status: '', q: '' });
    await fixture.componentInstance['loadTemplates']();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Chưa có đề nào');
    expect(element.textContent).toContain('Xóa bộ lọc');
  });

  it('blocks homework action for non-ready templates with ERR_TEMPLATE_NOT_READY message', () => {
    fixture.componentInstance['openMenuId'].set(draftTemplate.templateId);
    fixture.detectChanges();

    fixture.componentInstance['onHomeworkAction'](draftTemplate, new Event('click'));
    fixture.detectChanges();

    expect(fixture.componentInstance['blockedActionMessage']()).toBe(
      TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NOT_READY'],
    );
  });

  it('navigates draft templates to setup editor', async () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    await fixture.componentInstance['inspectTemplate'](draftTemplate, new Event('click'));

    expect(navigateSpy).toHaveBeenCalledWith(['/teacher/library', 'tpl-draft', 'setup']);
    expect(api.getTemplate).not.toHaveBeenCalled();
  });

  it('loads inspect panel when viewing template detail', async () => {
    await fixture.componentInstance['inspectTemplate'](readyTemplate, new Event('click'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect(api.getTemplate).toHaveBeenCalledWith('tpl-ready');
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Demo template');
    expect(element.textContent).toContain('Listening Ready');
  });

  it('closes action menu on escape key', () => {
    fixture.componentInstance['openMenuId'].set(draftTemplate.templateId);
    fixture.componentInstance['onEscapeKey']();
    expect(fixture.componentInstance['openMenuId']()).toBeNull();
  });

  it('syncs skill filter to query params', async () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    fixture.componentInstance['filters'].update((current) => ({ ...current, skill: 'listening' }));
    await fixture.componentInstance['syncQueryParamsAndLoad']();

    expect(navigateSpy).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        queryParams: expect.objectContaining({ skill: 'listening' }),
      }),
    );
  });
});
