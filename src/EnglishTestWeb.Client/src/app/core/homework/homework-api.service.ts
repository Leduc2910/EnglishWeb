import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CreateHomeworkRequest, HomeworkAssignment } from './homework.models';

@Injectable({ providedIn: 'root' })
export class HomeworkApiService {
  private readonly http = inject(HttpClient);

  create(request: CreateHomeworkRequest): Promise<HomeworkAssignment> {
    return firstValueFrom(
      this.http.post<HomeworkAssignment>('/api/homework-assignments', request),
    );
  }
}
