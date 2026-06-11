import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FilesApiService {
  private readonly http = inject(HttpClient);

  getContentUrl(fileId: string): string {
    return `/api/files/${fileId}/content`;
  }

  fetchContentBlob(fileId: string): Promise<Blob> {
    return firstValueFrom(
      this.http.get(this.getContentUrl(fileId), {
        responseType: 'blob',
      }),
    );
  }
}
