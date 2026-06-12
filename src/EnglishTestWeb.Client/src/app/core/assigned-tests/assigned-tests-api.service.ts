import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AssignedTestItem } from './assigned-tests.models';

interface AssignedTestsResponse {
  items: AssignedTestItem[];
}

@Injectable({ providedIn: 'root' })
export class AssignedTestsApiService {
  private readonly http = inject(HttpClient);

  getForActiveClass(): Promise<AssignedTestItem[]> {
    return firstValueFrom(
      this.http.get<AssignedTestsResponse>('/api/assigned-tests'),
    ).then((response) => response.items);
  }
}
