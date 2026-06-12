import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AssignedTestsApiService } from '../../core/assigned-tests/assigned-tests-api.service';
import {
  ASSIGNED_TEST_ERROR_MESSAGES,
  AssignedTestItem,
  STUDENT_STATUS_LABELS,
} from '../../core/assigned-tests/assigned-tests.models';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ClassContextService } from '../../core/classes/class-context.service';
import { SpeakingApiService } from '../../core/speaking/speaking-api.service';
import { SubmissionsApiService } from '../../core/submissions/submissions-api.service';

@Component({
  selector: 'app-student-assigned-tests',
  imports: [DatePipe],
  templateUrl: './student-assigned-tests.component.html',
  styleUrl: './student-assigned-tests.component.css',
})
export class StudentAssignedTestsComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly assignedTestsApi = inject(AssignedTestsApiService);
  private readonly submissionsApi = inject(SubmissionsApiService);
  private readonly speakingApi = inject(SpeakingApiService);
  protected readonly auth = inject(AuthSessionService);
  protected readonly classContext = inject(ClassContextService);

  protected readonly viewState = signal<'loading' | 'loaded' | 'error'>('loading');
  protected readonly homeworkItems = signal<AssignedTestItem[]>([]);
  protected readonly liveExamItems = signal<AssignedTestItem[]>([]);
  protected readonly activeTab = signal<'homework' | 'live-exam'>('homework');
  protected readonly skillFilter = signal<string>('all');
  protected readonly statusFilter = signal<string>('all');
  protected readonly blockedItemMessage = signal<string | null>(null);

  protected readonly statusLabels = STUDENT_STATUS_LABELS;

  protected readonly filteredHomework = computed(() => {
    const skill = this.skillFilter();
    const status = this.statusFilter();
    return this.homeworkItems().filter(
      (item) =>
        (skill === 'all' || item.skill === skill) &&
        (status === 'all' || item.studentStatus === status),
    );
  });

  protected readonly filteredLiveExams = computed(() => {
    const skill = this.skillFilter();
    const status = this.statusFilter();
    return this.liveExamItems().filter(
      (item) =>
        (skill === 'all' || item.skill === skill) &&
        (status === 'all' || item.studentStatus === status),
    );
  });

  async ngOnInit(): Promise<void> {
    await this.loadItems();
  }

  protected async loadItems(): Promise<void> {
    this.viewState.set('loading');
    this.blockedItemMessage.set(null);
    try {
      const items = await this.assignedTestsApi.getForActiveClass();
      this.homeworkItems.set(items.filter((i) => i.mode === 'homework'));
      this.liveExamItems.set(items.filter((i) => i.mode === 'live-exam'));
      this.viewState.set('loaded');
    } catch {
      this.viewState.set('error');
    }
  }

  protected onTabChange(tab: 'homework' | 'live-exam'): void {
    this.activeTab.set(tab);
    this.blockedItemMessage.set(null);
  }

  protected onSkillFilter(skill: string): void {
    this.skillFilter.set(skill);
  }

  protected onStatusFilter(status: string): void {
    this.statusFilter.set(status);
  }

  protected async onStartItem(item: AssignedTestItem): Promise<void> {
    if (item.studentStatus === 'not-open') {
      this.blockedItemMessage.set(ASSIGNED_TEST_ERROR_MESSAGES['ERR_LIVE_EXAM_NOT_OPEN']);
      return;
    }

    // Speaking submissions are always accessible even when expired/closed —
    // the page still shows the prompt and any existing draft (AC6).
    if (item.skill === 'speaking') {
      this.blockedItemMessage.set(null);
      const request =
        item.mode === 'homework'
          ? { homeworkAssignmentId: item.id, liveExamSessionId: null }
          : { homeworkAssignmentId: null, liveExamSessionId: item.id };
      try {
        const speakingSubmission = await this.speakingApi.createOrResume(request);
        await this.router.navigate(['/student/speaking', speakingSubmission.id]);
      } catch {
        this.blockedItemMessage.set('Không thể mở bài làm nói. Vui lòng thử lại.');
      }
      return;
    }

    if (item.studentStatus === 'expired') {
      this.blockedItemMessage.set(ASSIGNED_TEST_ERROR_MESSAGES['ERR_HOMEWORK_EXPIRED']);
      return;
    }
    if (item.studentStatus === 'closed') {
      this.blockedItemMessage.set(ASSIGNED_TEST_ERROR_MESSAGES['ERR_ITEM_CLOSED']);
      return;
    }
    this.blockedItemMessage.set(null);

    const request =
      item.mode === 'homework'
        ? { homeworkAssignmentId: item.id, liveExamSessionId: null }
        : { homeworkAssignmentId: null, liveExamSessionId: item.id };

    try {
      const submission = await this.submissionsApi.createOrResume(request);
      await this.router.navigate(['/student/workspace', submission.id]);
    } catch {
      this.blockedItemMessage.set('Không thể mở bài làm. Vui lòng thử lại.');
    }
  }

  protected async logout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/class']);
  }
}
