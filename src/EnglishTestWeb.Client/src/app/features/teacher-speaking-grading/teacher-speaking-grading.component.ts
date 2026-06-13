import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import { GradeSpeakingRequest, SPEAKING_ERROR_MESSAGES, TeacherSpeakingSubmissionDto } from '../../core/speaking/speaking.models';

type ViewState = 'loading' | 'loaded' | 'error';
type GradeState = 'idle' | 'submitting' | 'success' | 'error';

@Component({
  selector: 'app-teacher-speaking-grading',
  templateUrl: './teacher-speaking-grading.component.html',
  styleUrl: './teacher-speaking-grading.component.css',
  imports: [],
})
export class TeacherSpeakingGradingComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly speakingApi = inject(SpeakingApiService);
  private readonly sanitizer = inject(DomSanitizer);

  private submissionId: string | null = null;

  protected readonly viewState = signal<ViewState>('loading');
  protected readonly dto = signal<TeacherSpeakingSubmissionDto | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly scoreInput = signal<string>('');
  protected readonly feedbackInput = signal<string>('');
  protected readonly gradeState = signal<GradeState>('idle');
  protected readonly gradeErrorMessage = signal<string | null>(null);

  protected readonly audioUrl = computed((): SafeResourceUrl | null => {
    const id = this.submissionId;
    if (!id) return null;
    const url = this.speakingApi.getTeacherSubmissionFileUrl(id);
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  protected readonly modeLabel = computed(() => {
    const d = this.dto();
    if (!d) return '';
    return d.mode === 'homework' ? 'Bài tập về nhà' : 'Thi trực tiếp';
  });

  protected readonly statusLabel = computed(() => {
    const d = this.dto();
    if (!d) return '';
    if (d.status === 'draft') return 'Nháp';
    if (d.status === 'submitted') return 'Đã nộp';
    return 'Đã chấm';
  });

  protected readonly canGrade = computed(() => {
    const d = this.dto();
    return d !== null && (d.status === 'submitted' || d.status === 'graded');
  });

  ngOnInit(): void {
    this.submissionId = this.route.snapshot.paramMap.get('speakingSubmissionId');
    void this.loadSubmission();
  }

  private async loadSubmission(): Promise<void> {
    if (!this.submissionId) {
      this.viewState.set('error');
      this.errorMessage.set('Không tìm thấy bài làm nói.');
      return;
    }
    this.viewState.set('loading');
    try {
      const dto = await this.speakingApi.getForTeacher(this.submissionId);
      this.dto.set(dto);
      this.scoreInput.set(dto.score !== null ? String(dto.score) : '');
      if (dto.feedback) this.feedbackInput.set(dto.feedback);
      this.viewState.set('loaded');
    } catch {
      this.viewState.set('error');
      this.errorMessage.set('Không thể tải bài làm nói. Vui lòng thử lại.');
    }
  }

  protected async onGradeSubmit(): Promise<void> {
    if (this.gradeState() === 'submitting') return;
    const scoreStr = this.scoreInput().trim();
    const score = scoreStr === '' ? null : Number(scoreStr);
    if (score === null || !Number.isInteger(score) || score < 0 || score > 10) {
      this.gradeErrorMessage.set('Điểm số phải là số nguyên từ 0 đến 10.');
      return;
    }
    if (!this.submissionId) return;

    this.gradeState.set('submitting');
    this.gradeErrorMessage.set(null);

    const request: GradeSpeakingRequest = {
      score,
      feedback: this.feedbackInput().trim() || null,
    };

    try {
      const updated = await this.speakingApi.grade(this.submissionId, request);
      this.dto.set(updated);
      this.gradeState.set('success');
    } catch (err: unknown) {
      this.gradeState.set('error');
      const code = this.extractErrorCode(err);
      this.gradeErrorMessage.set(
        SPEAKING_ERROR_MESSAGES[code ?? ''] ?? 'Chấm điểm thất bại. Vui lòng thử lại.',
      );
    }
  }

  protected formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString('vi-VN');
  }

  private extractErrorCode(err: unknown): string | null {
    if (err && typeof err === 'object' && 'error' in err) {
      const body = (err as { error: unknown }).error;
      if (body && typeof body === 'object' && 'extensions' in body) {
        const ext = (body as { extensions: unknown }).extensions;
        if (ext && typeof ext === 'object' && 'code' in ext)
          return String((ext as { code: unknown }).code);
      }
    }
    return null;
  }
}
