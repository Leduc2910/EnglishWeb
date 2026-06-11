import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { startWith, Subscription } from 'rxjs';
import { TestTemplatesApiService } from '../../core/test-templates/test-templates-api.service';
import {
  mapTemplateApiError,
  parseTagsInput,
  skillChecklist,
  SkillChecklistItem,
  TEMPLATE_ERROR_MESSAGES,
  TemplateSkill,
} from '../../core/test-templates/test-templates.models';

type SaveState = 'idle' | 'saving' | 'saved' | 'error';

interface SetupSavePayload {
  title: string;
  skill: string;
  description: string | null;
  tags: string[];
}

@Component({
  selector: 'app-test-template-setup',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './test-template-setup.component.html',
  styleUrl: './test-template-setup.component.css',
})
export class TestTemplateSetupComponent implements OnInit, OnDestroy {
  private readonly api = inject(TestTemplatesApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  private paramSubscription?: Subscription;
  private saveChainPromise: Promise<string | null> | null = null;
  private pendingSavePayload: SetupSavePayload | null = null;
  private loadRequestId = 0;
  private skipLoadTemplateId: string | null = null;

  protected readonly templateId = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly saveState = signal<SaveState>('idle');
  protected readonly bannerError = signal<string | null>(null);
  protected readonly isEditMode = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
    skill: ['reading' as TemplateSkill, Validators.required],
    description: [''],
    tagsInput: [''],
  });

  private readonly selectedSkill = toSignal(
    this.form.controls.skill.valueChanges.pipe(startWith(this.form.controls.skill.value)),
    { initialValue: this.form.controls.skill.value },
  );

  protected readonly checklist = computed<SkillChecklistItem[]>(() =>
    skillChecklist(this.selectedSkill() ?? 'reading'),
  );

  protected readonly saveStatusLabel = computed(() => {
    switch (this.saveState()) {
      case 'saving':
        return 'Đang lưu nháp…';
      case 'saved':
        return 'Đã lưu nháp';
      case 'error':
        return 'Lưu nháp thất bại';
      default:
        return 'Chưa lưu';
    }
  });

  protected readonly isSaving = computed(() => this.saveState() === 'saving');

  protected readonly skillOptions: { value: TemplateSkill; label: string }[] = [
    { value: 'reading', label: 'Reading' },
    { value: 'listening', label: 'Listening' },
    { value: 'speaking', label: 'Speaking' },
  ];

  ngOnInit(): void {
    this.paramSubscription = this.route.paramMap.subscribe((params) => {
      const templateId = params.get('templateId');
      if (!templateId) {
        this.resetCreateMode();
        return;
      }

      if (this.skipLoadTemplateId === templateId) {
        this.skipLoadTemplateId = null;
        this.isEditMode.set(true);
        return;
      }

      void this.loadTemplate(templateId);
    });
  }

  ngOnDestroy(): void {
    this.paramSubscription?.unsubscribe();
  }

  protected titleError(): string | null {
    const control = this.form.controls.title;
    if (!control.touched && !control.dirty) {
      return null;
    }

    if (control.hasError('required') || control.hasError('minlength')) {
      return TEMPLATE_ERROR_MESSAGES['ERR_TEMPLATE_NAME_REQUIRED'];
    }

    if (control.hasError('maxlength')) {
      return TEMPLATE_ERROR_MESSAGES['templates.titleTooLong'];
    }

    return null;
  }

  protected skillError(): string | null {
    const control = this.form.controls.skill;
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return TEMPLATE_ERROR_MESSAGES['ERR_SKILL_REQUIRED'];
    }

    return null;
  }

  protected tagsError(): string | null {
    const tags = parseTagsInput(this.form.controls.tagsInput.value);
    if (tags.some((tag) => tag.length > 32)) {
      return TEMPLATE_ERROR_MESSAGES['templates.tagTooLong'];
    }

    if (tags.length > 10) {
      return TEMPLATE_ERROR_MESSAGES['ERR_TAG_LIMIT'];
    }

    return null;
  }

  protected requiredFieldsComplete(): boolean {
    const title = this.form.controls.title.value.trim();
    const skill = this.form.controls.skill.value;
    return title.length >= 3 && Boolean(skill) && !this.tagsError();
  }

  protected async onSaveDraft(): Promise<void> {
    await this.persistSetup(false);
  }

  protected async onContinue(): Promise<void> {
    const savedId = await this.persistSetup(true);
    if (savedId) {
      await this.router.navigate(['/teacher/library', savedId, 'materials']);
    }
  }

  protected async onBack(): Promise<void> {
    await this.router.navigate(['/teacher/library']);
  }

  private resetCreateMode(): void {
    this.loadRequestId++;
    this.isEditMode.set(false);
    this.templateId.set(null);
    this.loadError.set(null);
    this.isLoading.set(false);
    this.bannerError.set(null);
    this.skipLoadTemplateId = null;
    this.form.reset({
      title: '',
      skill: 'reading',
      description: '',
      tagsInput: '',
    });
    this.saveState.set('idle');
  }

  private async loadTemplate(templateId: string): Promise<void> {
    const requestId = ++this.loadRequestId;
    this.isEditMode.set(true);
    this.isLoading.set(true);
    this.loadError.set(null);
    this.bannerError.set(null);
    this.saveState.set('idle');

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
      this.form.reset({
        title: detail.title,
        skill: detail.skill as TemplateSkill,
        description: detail.description ?? '',
        tagsInput: detail.tags.join(', '),
      });
      this.saveState.set('saved');
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

  private async persistSetup(markContinueAttempt: boolean): Promise<string | null> {
    this.bannerError.set(null);
    this.form.markAllAsTouched();

    if (this.form.invalid || this.tagsError()) {
      if (this.saveState() === 'saved') {
        this.saveState.set('idle');
      }

      if (markContinueAttempt && this.form.controls.skill.invalid) {
        this.bannerError.set(TEMPLATE_ERROR_MESSAGES['ERR_SKILL_REQUIRED']);
      }

      return null;
    }

    const payload: SetupSavePayload = {
      title: this.form.controls.title.value.trim(),
      skill: this.form.controls.skill.value,
      description: this.form.controls.description.value.trim() || null,
      tags: parseTagsInput(this.form.controls.tagsInput.value),
    };

    if (this.saveChainPromise) {
      this.pendingSavePayload = payload;
      return this.saveChainPromise;
    }

    this.saveChainPromise = this.runSaveChain(payload);

    try {
      return await this.saveChainPromise;
    } finally {
      this.saveChainPromise = null;
    }
  }

  private async runSaveChain(initialPayload: SetupSavePayload): Promise<string | null> {
    let payload = initialPayload;
    let result: string | null = null;

    while (true) {
      this.saveState.set('saving');
      result = await this.executeSave(payload);

      if (!this.pendingSavePayload) {
        return result;
      }

      payload = this.pendingSavePayload;
      this.pendingSavePayload = null;
    }
  }

  private async executeSave(payload: SetupSavePayload): Promise<string | null> {
    try {
      const existingId = this.templateId();
      const response = existingId
        ? await this.api.updateTemplate(existingId, payload)
        : await this.api.createTemplate(payload);

      this.templateId.set(response.templateId);
      this.saveState.set('saved');

      if (!existingId) {
        this.skipLoadTemplateId = response.templateId;
        await this.router.navigate(['/teacher/library', response.templateId, 'setup'], {
          replaceUrl: true,
        });
        this.isEditMode.set(true);
      }

      return response.templateId;
    } catch (error) {
      this.saveState.set('error');
      this.bannerError.set(mapTemplateApiError(error));
      return null;
    }
  }
}
