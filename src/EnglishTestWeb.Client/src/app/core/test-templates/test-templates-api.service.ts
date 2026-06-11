import { HttpClient, HttpEventType, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { filter, firstValueFrom, map } from 'rxjs';
import {
  MaterialRole,
  TestMaterialItem,
  TestMaterialListResponse,
  TestTemplateDetail,
  TestTemplateListFilters,
  TestTemplateListItem,
  TestTemplateSetupPayload,
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

  createTemplate(payload: TestTemplateSetupPayload): Promise<TestTemplateDetail> {
    return firstValueFrom(
      this.http.post<TestTemplateDetail>('/api/test-templates', payload),
    );
  }

  updateTemplate(templateId: string, payload: TestTemplateSetupPayload): Promise<TestTemplateDetail> {
    return firstValueFrom(
      this.http.put<TestTemplateDetail>(`/api/test-templates/${templateId}`, payload),
    );
  }

  listMaterials(templateId: string): Promise<TestMaterialItem[]> {
    return firstValueFrom(
      this.http
        .get<TestMaterialListResponse>(`/api/test-templates/${templateId}/materials`)
        .pipe(map((response) => response.items)),
    );
  }

  uploadMaterial(
    templateId: string,
    role: MaterialRole,
    file: File,
    onProgress?: (percent: number) => void,
  ): Promise<TestMaterialItem> {
    const formData = new FormData();
    formData.append('role', role);
    formData.append('file', file, file.name);

    return firstValueFrom(
      this.http
        .post<TestMaterialItem>(`/api/test-templates/${templateId}/materials`, formData, {
          reportProgress: true,
          observe: 'events',
        })
        .pipe(
          map((event) => {
            if (event.type === HttpEventType.UploadProgress && event.total) {
              onProgress?.(Math.round((100 * event.loaded) / event.total));
            }

            if (event.type === HttpEventType.Response) {
              return event.body!;
            }

            return null;
          }),
          filter((value): value is TestMaterialItem => value !== null),
        ),
    );
  }

  removeMaterial(templateId: string, materialId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`/api/test-templates/${templateId}/materials/${materialId}`),
    );
  }
}
