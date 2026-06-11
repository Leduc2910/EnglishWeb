import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CreateLiveExamRequest, LiveExamSession } from './live-exam.models';

@Injectable({ providedIn: 'root' })
export class LiveExamApiService {
  private readonly http = inject(HttpClient);

  create(request: CreateLiveExamRequest): Promise<LiveExamSession> {
    return firstValueFrom(
      this.http.post<LiveExamSession>('/api/live-exam-sessions', request),
    );
  }

  open(id: string): Promise<LiveExamSession> {
    return firstValueFrom(
      this.http.post<LiveExamSession>(`/api/live-exam-sessions/${id}/open`, {}),
    );
  }

  close(id: string): Promise<LiveExamSession> {
    return firstValueFrom(
      this.http.post<LiveExamSession>(`/api/live-exam-sessions/${id}/close`, {}),
    );
  }
}
