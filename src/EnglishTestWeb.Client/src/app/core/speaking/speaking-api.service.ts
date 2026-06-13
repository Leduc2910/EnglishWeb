import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  CreateSpeakingSubmissionRequest,
  GradeSpeakingRequest,
  SpeakingSubmissionDto,
  TeacherSpeakingSubmissionDto,
} from './speaking.models';

@Injectable({ providedIn: 'root' })
export class SpeakingApiService {
  private readonly http = inject(HttpClient);

  createOrResume(request: CreateSpeakingSubmissionRequest): Promise<SpeakingSubmissionDto> {
    return firstValueFrom(
      this.http.post<SpeakingSubmissionDto>('/api/speaking-submissions', request),
    );
  }

  get(speakingSubmissionId: string): Promise<SpeakingSubmissionDto> {
    return firstValueFrom(
      this.http.get<SpeakingSubmissionDto>(`/api/speaking-submissions/${speakingSubmissionId}`),
    );
  }

  uploadDraft(speakingSubmissionId: string, file: File): Promise<SpeakingSubmissionDto> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(
      this.http.post<SpeakingSubmissionDto>(
        `/api/speaking-submissions/${speakingSubmissionId}/upload-draft`,
        formData,
      ),
    );
  }

  finalSubmit(speakingSubmissionId: string): Promise<SpeakingSubmissionDto> {
    return firstValueFrom(
      this.http.post<SpeakingSubmissionDto>(
        `/api/speaking-submissions/${speakingSubmissionId}/final-submit`,
        {},
      ),
    );
  }

  getForTeacher(speakingSubmissionId: string): Promise<TeacherSpeakingSubmissionDto> {
    return firstValueFrom(
      this.http.get<TeacherSpeakingSubmissionDto>(
        `/api/teacher/speaking-submissions/${speakingSubmissionId}`,
      ),
    );
  }

  grade(speakingSubmissionId: string, request: GradeSpeakingRequest): Promise<TeacherSpeakingSubmissionDto> {
    return firstValueFrom(
      this.http.post<TeacherSpeakingSubmissionDto>(
        `/api/teacher/speaking-submissions/${speakingSubmissionId}/grade`,
        request,
      ),
    );
  }

  getTeacherSubmissionFileUrl(speakingSubmissionId: string): string {
    return `/api/teacher/speaking-submissions/${speakingSubmissionId}/file`;
  }

}
