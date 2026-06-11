import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { TestTemplateMaterialsComponent } from './test-template-materials.component';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import { FilesApiService } from '../../core/files/files-api.service';
import { TEMPLATE_ERROR_MESSAGES } from '../../core/test-templates/test-templates.models';

describe('TestTemplateMaterialsComponent', () => {
  let fixture: ComponentFixture<TestTemplateMaterialsComponent>;
  let api: {
    getTemplate: ReturnType<typeof vi.fn>;
    listMaterials: ReturnType<typeof vi.fn>;
    uploadMaterial: ReturnType<typeof vi.fn>;
    removeMaterial: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    api = {
      getTemplate: vi.fn().mockResolvedValue({
        templateId: 'tpl-1',
        title: 'Reading Draft',
        skill: 'reading',
        description: null,
        tags: [],
        status: 'draft',
        createdAt: '2026-06-10T00:00:00Z',
        updatedAt: '2026-06-10T00:00:00Z',
        lastUsedAt: null,
        archivedAt: null,
      }),
      listMaterials: vi.fn().mockResolvedValue([]),
      uploadMaterial: vi.fn(),
      removeMaterial: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TestTemplateMaterialsComponent],
      providers: [
        provideRouter([
          {
            path: 'teacher/library/:templateId/answer-key',
            component: TestTemplateMaterialsComponent,
          },
        ]),
        { provide: TestTemplatesApiService, useValue: api },
        {
          provide: FilesApiService,
          useValue: {
            fetchContentBlob: vi.fn().mockResolvedValue(new Blob(['pdf'], { type: 'application/pdf' })),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ templateId: 'tpl-1' })),
            snapshot: {
              paramMap: convertToParamMap({ templateId: 'tpl-1' }),
            },
          },
        },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(TestTemplateMaterialsComponent);
    fixture.detectChanges();
    return fixture.whenStable();
  });

  it('disables continue until required PDF uploaded', async () => {
    await fixture.whenStable();
    expect(fixture.componentInstance['canContinue']()).toBe(false);
  });

  it('rejects invalid file type client-side', async () => {
    await fixture.whenStable();
    const file = new File(['text'], 'notes.txt', { type: 'text/plain' });
    await fixture.componentInstance['uploadFile']('pdf', file);
    fixture.detectChanges();

    const slot = fixture.componentInstance['slots']().find((item) => item.role === 'pdf');
    expect(slot?.uploadError).toBe(TEMPLATE_ERROR_MESSAGES['ERR_FILE_TYPE']);
    expect(api.uploadMaterial).not.toHaveBeenCalled();
  });

  it('enables continue after successful PDF upload', async () => {
    api.uploadMaterial.mockResolvedValue({
      materialId: 'mat-1',
      fileId: 'file-1',
      role: 'pdf',
      originalFileName: 'sample.pdf',
      sizeBytes: 100,
      contentType: 'application/pdf',
      uploadedAt: '2026-06-10T00:00:00Z',
    });

    await fixture.whenStable();
    const file = new File(['%PDF-1.4'], 'sample.pdf', { type: 'application/pdf' });
    await fixture.componentInstance['uploadFile']('pdf', file);
    fixture.detectChanges();

    expect(fixture.componentInstance['canContinue']()).toBe(true);
  });

  it('keeps continue disabled while upload in progress', async () => {
    api.uploadMaterial.mockImplementation(
      (_templateId: string, _role: string, _file: File, onProgress?: (percent: number) => void) => {
        onProgress?.(40);
        return new Promise(() => undefined);
      },
    );

    await fixture.whenStable();
    const file = new File(['%PDF-1.4'], 'sample.pdf', { type: 'application/pdf' });
    void fixture.componentInstance['uploadFile']('pdf', file);
    fixture.detectChanges();

    expect(fixture.componentInstance['canContinue']()).toBe(false);
    expect(fixture.componentInstance['isAnyUploading']()).toBe(true);
  });

  it('opens preview only after successful upload', async () => {
    api.uploadMaterial.mockResolvedValue({
      materialId: 'mat-1',
      fileId: 'file-1',
      role: 'pdf',
      originalFileName: 'sample.pdf',
      sizeBytes: 100,
      contentType: 'application/pdf',
      uploadedAt: '2026-06-10T00:00:00Z',
    });

    await fixture.whenStable();
    const file = new File(['%PDF-1.4'], 'sample.pdf', { type: 'application/pdf' });
    await fixture.componentInstance['uploadFile']('pdf', file);
    const material = fixture.componentInstance['slots']().find((item) => item.role === 'pdf')?.material;
    expect(material).toBeTruthy();

    await fixture.componentInstance['onPreview'](material!);
    fixture.detectChanges();

    expect(fixture.componentInstance['previewUrl']()).toBeTruthy();
  });
});
