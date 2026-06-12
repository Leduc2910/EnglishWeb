import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import {
  ALLOWED_SPEAKING_MIME_TYPES,
  MAX_SPEAKING_FILE_SIZE_BYTES,
  SPEAKING_ERROR_MESSAGES,
  SpeakingSubmissionDto,
} from '../../core/speaking/speaking.models';

type ViewState = 'loading' | 'loaded' | 'error';
type UploadState = 'idle' | 'uploading' | 'error';

@Component({
  selector: 'app-student-speaking-submission',
  templateUrl: './student-speaking-submission.component.html',
  styleUrl: './student-speaking-submission.component.css',
})
export class StudentSpeakingSubmissionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly speakingApi = inject(SpeakingApiService);

  private submissionId: string | null = null;

  protected readonly viewState = signal<ViewState>('loading');
  protected readonly dto = signal<SpeakingSubmissionDto | null>(null);
  protected readonly errorCode = signal<string | null>(null);
  protected readonly uploadState = signal<UploadState>('idle');
  protected readonly uploadErrorCode = signal<string | null>(null);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly clientValidationError = signal<string | null>(null);

  protected readonly errorMessage = computed(() => {
    const code = this.errorCode();
    if (!code) return 'Không thể tải bài làm nói. Vui lòng thử lại.';
    return SPEAKING_ERROR_MESSAGES[code] ?? 'Không thể tải bài làm nói. Vui lòng thử lại.';
  });

  protected readonly uploadErrorMessage = computed(() => {
    const code = this.uploadErrorCode();
    if (!code) return 'Tải file thất bại. Vui lòng thử lại.';
    return SPEAKING_ERROR_MESSAGES[code] ?? 'Tải file thất bại. Vui lòng thử lại.';
  });

  protected readonly canUpload = computed(() => {
    const d = this.dto();
    return d !== null && d.status === 'draft' && d.isSourceOpen;
  });

  protected readonly modeLabel = computed(() => {
    const d = this.dto();
    if (!d) return '';
    return d.mode === 'homework' ? 'Bài tập về nhà' : 'Thi trực tiếp';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('speakingSubmissionId');
    if (!id) {
      void this.router.navigate(['/student/tests']);
      return;
    }
    this.submissionId = id;
    void this.load(id);
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(null);
    this.clientValidationError.set(null);
    this.uploadErrorCode.set(null);

    if (!file) return;

    if (!ALLOWED_SPEAKING_MIME_TYPES.includes(file.type)) {
      this.clientValidationError.set(
        'Loại file không được hỗ trợ. Vui lòng chọn file âm thanh hoặc video.',
      );
      return;
    }

    if (file.size > MAX_SPEAKING_FILE_SIZE_BYTES) {
      this.clientValidationError.set('File vượt quá giới hạn 100MB.');
      return;
    }

    this.selectedFile.set(file);
  }

  protected async onUpload(): Promise<void> {
    const id = this.submissionId;
    const file = this.selectedFile();
    if (!id || !file) return;

    this.uploadState.set('uploading');
    this.uploadErrorCode.set(null);

    try {
      const updated = await this.speakingApi.uploadDraft(id, file);
      this.dto.set(updated);
      this.selectedFile.set(null);
      this.uploadState.set('idle');
    } catch (err: unknown) {
      const code = this.extractErrorCode(err);
      this.uploadErrorCode.set(code);
      this.uploadState.set('error');
    }
  }

  protected formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1_048_576) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1_048_576).toFixed(1)} MB`;
  }

  protected formatDate(iso: string): string {
    return new Intl.DateTimeFormat('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(iso));
  }

  protected backToTests(): void {
    void this.router.navigate(['/student/tests']);
  }

  protected retryLoad(): void {
    if (!this.submissionId) return;
    void this.load(this.submissionId);
  }

  private async load(id: string): Promise<void> {
    this.viewState.set('loading');
    this.dto.set(null);
    this.errorCode.set(null);

    try {
      const data = await this.speakingApi.get(id);
      this.dto.set(data);
      this.viewState.set('loaded');
    } catch (err: unknown) {
      this.errorCode.set(this.extractErrorCode(err));
      this.viewState.set('error');
    }
  }

  private extractErrorCode(err: unknown): string | null {
    if (err && typeof err === 'object' && 'error' in err) {
      const body = (err as { error: unknown }).error;
      if (body && typeof body === 'object' && 'extensions' in body) {
        const ext = (body as { extensions: unknown }).extensions;
        if (ext && typeof ext === 'object' && 'code' in ext) {
          return String((ext as { code: unknown }).code);
        }
      }
    }
    return null;
  }
}
