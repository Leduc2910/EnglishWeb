import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TeacherDashboardDto } from './dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getDashboard(classId?: string): Promise<TeacherDashboardDto> {
    const params: Record<string, string> = {};
    if (classId) params['classId'] = classId;
    return firstValueFrom(
      this.http.get<TeacherDashboardDto>('/api/teacher/dashboard', { params }),
    );
  }
}
