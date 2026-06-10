import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ClassCurrent, ClassDetail, ClassLookupPreview, ClassSummary } from './classes.models';

@Injectable({ providedIn: 'root' })
export class ClassesApiService {
  private readonly http = inject(HttpClient);

  lookupByCode(code: string): Promise<ClassLookupPreview> {
    return firstValueFrom(this.http.get<ClassLookupPreview>(`/api/classes/by-code/${encodeURIComponent(code)}`));
  }

  getTeacherClasses(): Promise<ClassSummary[]> {
    return firstValueFrom(this.http.get<ClassSummary[]>('/api/classes'));
  }

  getClassDetail(classId: string): Promise<ClassDetail> {
    return firstValueFrom(this.http.get<ClassDetail>(`/api/classes/${classId}`));
  }

  getCurrentClass(): Promise<ClassCurrent> {
    return firstValueFrom(this.http.get<ClassCurrent>('/api/classes/current'));
  }
}
