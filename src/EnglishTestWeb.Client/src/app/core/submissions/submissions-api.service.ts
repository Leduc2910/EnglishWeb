import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AutosaveAnswersRow, CreateSubmissionRequest, SubmissionDto, SubmissionResultDto, SubmissionWorkspace } from './submissions.models';

@Injectable({ providedIn: 'root' })
export class SubmissionsApiService {
  private readonly http = inject(HttpClient);

  createOrResume(request: CreateSubmissionRequest): Promise<SubmissionDto> {
    return firstValueFrom(this.http.post<SubmissionDto>('/api/submissions', request));
  }

  getWorkspace(submissionId: string): Promise<SubmissionWorkspace> {
    return firstValueFrom(this.http.get<SubmissionWorkspace>(`/api/submissions/${submissionId}/workspace`));
  }

  getMaterialContentUrl(submissionId: string, fileId: string): string {
    return `/api/submissions/${submissionId}/materials/${fileId}/content`;
  }

  autosaveAnswers(submissionId: string, rows: AutosaveAnswersRow[]): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`/api/submissions/${submissionId}/answers`, { rows }),
    );
  }

  finalSubmit(submissionId: string): Promise<SubmissionResultDto> {
    return firstValueFrom(
      this.http.post<SubmissionResultDto>(`/api/submissions/${submissionId}/submit`, {}),
    );
  }
}
