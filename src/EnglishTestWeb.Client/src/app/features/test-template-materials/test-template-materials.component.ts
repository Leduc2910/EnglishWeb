import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { FilesApiService } from '../../core/files/files-api.service';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import {
  mapMaterialApiError,
  mapTemplateApiError,
  MaterialRole,
  materialContinueRequiredMessage,
  materialSlotsForSkill,
  previewLabelForRole,
  skillChecklist,
  TEMPLATE_ERROR_MESSAGES,
  TestMaterialItem,
  validateMaterialFile,
} from '../../core/test-templates/test-templates.models';

interface SlotState {
  role: MaterialRole;
  label: string;
  required: boolean;
  accept: string;
  pickerLabel: string;
  material: TestMaterialItem | null;
  uploadProgress: number | null;
  uploadError: string | null;
  isUploading: boolean;
}

@Component({
  selector: 'app-test-template-materials',
  imports: [RouterLink],
  templateUrl: './test-template-materials.component.html',
  styleUrl: './test-template-materials.component.css',
})
export class TestTemplateMaterialsComponent implements OnInit, OnDestroy {
  private readonly api = inject(TestTemplatesApiService);
  private readonly filesApi = inject(FilesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly sanitizer = inject(DomSanitizer);

  private paramSubscription?: Subscription;
  private loadRequestId = 0;
  private previewObjectUrl: string | null = null;

  protected readonly templateId = signal<string | null>(null);
  protected readonly templateTitle = signal('');
  protected readonly templateSkill = signal('reading');
  protected readonly isLoading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly bannerError = signal<string | null>(null);
  protected readonly slots = signal<SlotState[]>([]);
  protected readonly previewUrl = signal<string | null>(null);
  protected readonly previewFileName = signal<string | null>(null);
  protected readonly previewIsAudio = signal(false);
  protected readonly previewLabelForRole = previewLabelForRole;
  protected readonly safePreviewUrl = computed<SafeResourceUrl | null>(() => {
    const url = this.previewUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  protected readonly checklist = computed(() => skillChecklist(this.templateSkill()));

  protected readonly uploadStatusLabel = computed(() => {
    if (this.slots().some((slot) => slot.isUploading)) {
      return 'Đang upload…';
    }

    if (this.slots().some((slot) => slot.uploadError)) {
      return 'Upload lỗi';
    }

    if (this.requiredSlotsSatisfied()) {
      return 'Đã upload tài liệu bắt buộc';
    }

    return 'Chờ upload';
  });

  protected readonly isAnyUploading = computed(() =>
    this.slots().some((slot) => slot.isUploading),
  );

  protected readonly canContinue = computed(
    () => this.requiredSlotsSatisfied() && !this.isAnyUploading(),
  );

  ngOnInit(): void {
    this.paramSubscription = this.route.paramMap.subscribe((params) => {
      const templateId = params.get('templateId');
      if (!templateId) {
        void this.router.navigate(['/teacher/library']);
        return;
      }

      void this.loadPage(templateId);
    });
  }

  ngOnDestroy(): void {
    this.paramSubscription?.unsubscribe();
    this.revokePreviewUrl();
  }

  protected requiredSlotsSatisfied(): boolean {
    const requiredSlots = this.slots().filter((slot) => slot.required);
    if (requiredSlots.length === 0) {
      return false;
    }

    return requiredSlots.every((slot) => slot.material !== null && !slot.isUploading);
  }

  protected formatBytes(size: number): string {
    if (size < 1024) {
      return `${size} B`;
    }

    if (size < 1024 * 1024) {
      return `${(size / 1024).toFixed(1)} KB`;
    }

    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }

  protected onFileSelected(role: MaterialRole, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }

    void this.uploadFile(role, file);
  }

  protected onDrop(role: MaterialRole, event: DragEvent): void {
    event.preventDefault();
    if (this.isAnyUploading()) {
      this.bannerError.set(TEMPLATE_ERROR_MESSAGES['ERR_UPLOAD_INCOMPLETE']);
      return;
    }

    const file = event.dataTransfer?.files?.[0];
    if (!file) {
      return;
    }

    void this.uploadFile(role, file);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  protected async onRemove(role: MaterialRole): Promise<void> {
    const templateId = this.templateId();
    const slot = this.slots().find((item) => item.role === role);
    if (!templateId || !slot?.material) {
      return;
    }

    if (!confirm('Xóa file đã upload?')) {
      return;
    }

    this.bannerError.set(null);
    try {
      await this.api.removeMaterial(templateId, slot.material.materialId);
      this.updateSlot(role, (current) => ({
        ...current,
        material: null,
        uploadError: null,
        uploadProgress: null,
        isUploading: false,
      }));
    } catch (error) {
      this.bannerError.set(mapMaterialApiError(error));
    }
  }

  protected async onPreview(material: TestMaterialItem): Promise<void> {
    this.revokePreviewUrl();
    try {
      const blob = await this.filesApi.fetchContentBlob(material.fileId);
      const objectUrl = URL.createObjectURL(blob);
      this.previewObjectUrl = objectUrl;
      this.previewUrl.set(objectUrl);
      this.previewFileName.set(material.originalFileName);
      this.previewIsAudio.set(material.role === 'audio' || material.contentType.startsWith('audio/'));
    } catch (error) {
      this.bannerError.set(mapMaterialApiError(error));
    }
  }

  protected closePreview(): void {
    this.revokePreviewUrl();
    this.previewUrl.set(null);
    this.previewFileName.set(null);
    this.previewIsAudio.set(false);
  }

  protected async onSaveDraft(): Promise<void> {
    const templateId = this.templateId();
    if (!templateId) {
      return;
    }

    this.bannerError.set(null);
    try {
      await this.refreshMaterials(templateId);
    } catch (error) {
      this.bannerError.set(mapMaterialApiError(error));
    }
  }

  protected async onContinue(): Promise<void> {
    if (!this.canContinue()) {
      this.bannerError.set(materialContinueRequiredMessage(this.templateSkill()));
      return;
    }

    const templateId = this.templateId();
    if (!templateId) {
      return;
    }

    await this.router.navigate(['/teacher/library', templateId, 'answer-key']);
  }

  protected async onBack(): Promise<void> {
    const templateId = this.templateId();
    if (templateId) {
      await this.router.navigate(['/teacher/library', templateId, 'setup']);
      return;
    }

    await this.router.navigate(['/teacher/library']);
  }

  private async loadPage(templateId: string): Promise<void> {
    const requestId = ++this.loadRequestId;
    this.isLoading.set(true);
    this.loadError.set(null);
    this.bannerError.set(null);

    try {
      const detail = await this.api.getTemplate(templateId);
      if (requestId !== this.loadRequestId) {
        return;
      }

      if (detail.status !== 'draft') {
        this.loadError.set(TEMPLATE_ERROR_MESSAGES['templates.notEditable']);
        this.templateId.set(null);
        return;
      }

      this.templateId.set(detail.templateId);
      this.templateTitle.set(detail.title);
      this.templateSkill.set(detail.skill);
      this.slots.set(
        materialSlotsForSkill(detail.skill).map((slot) => ({
          ...slot,
          material: null,
          uploadProgress: null,
          uploadError: null,
          isUploading: false,
        })),
      );

      await this.refreshMaterials(templateId, requestId);
    } catch (error) {
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.loadError.set(mapTemplateApiError(error));
      this.templateId.set(null);
    } finally {
      if (requestId === this.loadRequestId) {
        this.isLoading.set(false);
      }
    }
  }

  private async refreshMaterials(templateId: string, requestId = this.loadRequestId): Promise<void> {
    try {
      const materials = await this.api.listMaterials(templateId);
      if (requestId !== this.loadRequestId) {
        return;
      }

      this.slots.update((current) =>
        current.map((slot) => ({
          ...slot,
          material: materials.find((item) => item.role === slot.role) ?? null,
          uploadError: null,
          uploadProgress: null,
          isUploading: false,
        })),
      );
    } catch (error) {
      if (requestId !== this.loadRequestId) {
        return;
      }

      throw error;
    }
  }

  private async uploadFile(role: MaterialRole, file: File): Promise<void> {
    const templateId = this.templateId();
    if (!templateId || this.isAnyUploading()) {
      return;
    }

    const validationError = validateMaterialFile(role, file);
    if (validationError) {
      this.updateSlot(role, (slot) => ({
        ...slot,
        uploadError: validationError,
        uploadProgress: null,
        isUploading: false,
      }));
      return;
    }

    this.bannerError.set(null);
    this.updateSlot(role, (slot) => ({
      ...slot,
      uploadError: null,
      uploadProgress: 0,
      isUploading: true,
    }));

    try {
      const material = await this.api.uploadMaterial(templateId, role, file, (percent) => {
        this.updateSlot(role, (slot) => ({
          ...slot,
          uploadProgress: percent,
        }));
      });

      this.updateSlot(role, (slot) => ({
        ...slot,
        material,
        uploadError: null,
        uploadProgress: 100,
        isUploading: false,
      }));
    } catch (error) {
      this.updateSlot(role, (slot) => ({
        ...slot,
        uploadError: mapMaterialApiError(error),
        uploadProgress: null,
        isUploading: false,
      }));
    }
  }

  private updateSlot(role: MaterialRole, updater: (slot: SlotState) => SlotState): void {
    this.slots.update((current) =>
      current.map((slot) => (slot.role === role ? updater(slot) : slot)),
    );
  }

  private revokePreviewUrl(): void {
    if (this.previewObjectUrl) {
      URL.revokeObjectURL(this.previewObjectUrl);
      this.previewObjectUrl = null;
    }
  }
}
