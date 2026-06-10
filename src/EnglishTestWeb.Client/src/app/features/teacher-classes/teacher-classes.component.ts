import { Component, inject, OnInit, signal } from '@angular/core';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { ClassDetail, ClassSummary } from '../../core/classes/classes.models';

@Component({
  selector: 'app-teacher-classes',
  templateUrl: './teacher-classes.component.html',
  styleUrl: './teacher-classes.component.css',
})
export class TeacherClassesComponent implements OnInit {
  private readonly classesApi = inject(ClassesApiService);

  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly classes = signal<ClassSummary[]>([]);
  protected readonly selectedDetail = signal<ClassDetail | null>(null);

  async ngOnInit(): Promise<void> {
    await this.loadClasses();
  }

  protected async loadClasses(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const summaries = await this.classesApi.getTeacherClasses();
      this.classes.set(summaries);

      if (summaries.length > 0) {
        const detail = await this.classesApi.getClassDetail(summaries[0].classId);
        this.selectedDetail.set(detail);
      }
    } catch {
      this.errorMessage.set('Không thể tải danh sách lớp. Vui lòng thử lại.');
    } finally {
      this.isLoading.set(false);
    }
  }

  protected statusLabel(status: string): string {
    return status === 'active' ? 'Đang hoạt động' : 'Không hoạt động';
  }
}
