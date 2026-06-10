import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  TestTemplateDetail,
  TestTemplateListFilters,
  TestTemplateListItem,
} from './test-templates.models';

@Injectable({ providedIn: 'root' })
export class TestTemplatesApiService {
  private readonly http = inject(HttpClient);

  listTemplates(filters: TestTemplateListFilters): Promise<TestTemplateListItem[]> {
    let params = new HttpParams();

    if (filters.skill) {
      params = params.set('skill', filters.skill);
    }

    if (filters.status) {
      params = params.set('status', filters.status);
    }

    if (filters.q.trim()) {
      params = params.set('q', filters.q.trim());
    }

    return firstValueFrom(
      this.http.get<TestTemplateListItem[]>('/api/test-templates', { params }),
    );
  }

  getTemplate(templateId: string): Promise<TestTemplateDetail> {
    return firstValueFrom(
      this.http.get<TestTemplateDetail>(`/api/test-templates/${templateId}`),
    );
  }
}
