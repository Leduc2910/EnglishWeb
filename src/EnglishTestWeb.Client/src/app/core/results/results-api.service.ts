import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ResultsFilter, ResultsPageDto, TeacherSubmissionDetailDto } from './results.models';

@Injectable({ providedIn: 'root' })
export class ResultsApiService {
  private readonly http = inject(HttpClient);

  getResults(filter: ResultsFilter): Promise<ResultsPageDto> {
    let params = new HttpParams()
      .set('page', filter.page)
      .set('pageSize', filter.pageSize)
      .set('sort', filter.sort)
      .set('direction', filter.direction);

    if (filter.classId)    params = params.set('classId', filter.classId);
    if (filter.mode)       params = params.set('mode', filter.mode);
    if (filter.templateId) params = params.set('templateId', filter.templateId);
    if (filter.q)          params = params.set('q', filter.q);
    if (filter.skill)      params = params.set('skill', filter.skill);
    if (filter.status)     params = params.set('status', filter.status);

    return firstValueFrom(
      this.http.get<ResultsPageDto>('/api/teacher/results', { params }),
    );
  }

  getSubmissionDetail(submissionId: string): Promise<TeacherSubmissionDetailDto> {
    return firstValueFrom(
      this.http.get<TeacherSubmissionDetailDto>(
        `/api/teacher/results/submissions/${submissionId}`,
      ),
    );
  }
}
