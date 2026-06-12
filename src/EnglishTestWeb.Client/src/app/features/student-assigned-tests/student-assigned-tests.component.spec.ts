import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { StudentAssignedTestsComponent } from './student-assigned-tests.component';
import { AssignedTestsApiService } from '../../core/assigned-tests/assigned-tests-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ClassContextService } from '../../core/classes/class-context.service';
import { AssignedTestItem } from '../../core/assigned-tests/assigned-tests.models';

async function flushPromises(): Promise<void> {
  await new Promise<void>((r) => setTimeout(r, 0));
}

function makeHomeworkItem(overrides: Partial<AssignedTestItem> = {}): AssignedTestItem {
  return {
    id: 'hw-1',
    mode: 'homework',
    title: 'Reading Test',
    skill: 'reading',
    classId: 'cls-1',
    className: 'Lớp 7A',
    status: 'published',
    studentStatus: 'available',
    deadlineAt: '2026-12-31T12:00:00Z',
    timeLimitMinutes: null,
    scheduledStartAt: null,
    openedAt: null,
    closedAt: null,
    createdAt: '2026-06-10T00:00:00Z',
    ...overrides,
  };
}

function makeLiveExamItem(overrides: Partial<AssignedTestItem> = {}): AssignedTestItem {
  return {
    id: 'le-1',
    mode: 'live-exam',
    title: 'Listening Exam',
    skill: 'listening',
    classId: 'cls-1',
    className: 'Lớp 7A',
    status: 'scheduled',
    studentStatus: 'not-open',
    deadlineAt: null,
    timeLimitMinutes: null,
    scheduledStartAt: '2026-12-31T09:00:00Z',
    openedAt: null,
    closedAt: null,
    createdAt: '2026-06-10T00:00:00Z',
    ...overrides,
  };
}

describe('StudentAssignedTestsComponent', () => {
  let fixture: ComponentFixture<StudentAssignedTestsComponent>;
  let component: StudentAssignedTestsComponent;
  let assignedTestsApi: { getForActiveClass: ReturnType<typeof vi.fn> };

  async function setup(items: AssignedTestItem[] = []): Promise<void> {
    assignedTestsApi = {
      getForActiveClass: vi.fn().mockResolvedValue(items),
    };

    const authMock = {
      currentUser: () => ({ userName: 'Student User', email: 'student@test.local', roles: ['Student'] }),
      logout: vi.fn().mockResolvedValue(undefined),
    };
    const classContextMock = {
      activeClass: () => ({ className: 'Lớp 7A', classId: 'cls-1', classCode: 'ENG7A' }),
      confirmedClass: () => null,
    };

    await TestBed.configureTestingModule({
      imports: [StudentAssignedTestsComponent],
      providers: [
        provideRouter([]),
        { provide: AssignedTestsApiService, useValue: assignedTestsApi },
        { provide: AuthSessionService, useValue: authMock },
        { provide: ClassContextService, useValue: classContextMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentAssignedTestsComponent);
    component = fixture.componentInstance;
  }

  async function initAndLoad(): Promise<void> {
    fixture.detectChanges();
    await flushPromises();
    fixture.detectChanges();
  }

  it('tải được danh sách và phân loại vào hai tab', async () => {
    const items = [makeHomeworkItem(), makeLiveExamItem()];
    await setup(items);
    await initAndLoad();

    expect((component as any).homeworkItems().length).toBe(1);
    expect((component as any).liveExamItems().length).toBe(1);
    expect((component as any).viewState()).toBe('loaded');
  });

  it('empty state hiển thị đúng khi không có bài', async () => {
    await setup([]);
    await initAndLoad();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('Lớp 7A');
  });

  it('homework expired — nút bị disabled', async () => {
    const expiredItem = makeHomeworkItem({ studentStatus: 'expired', id: 'hw-expired' });
    await setup([expiredItem]);
    await initAndLoad();

    const startButton: HTMLButtonElement = fixture.nativeElement.querySelector('.start-button');
    expect(startButton).toBeTruthy();
    expect(startButton.disabled).toBe(true);
  });

  it('live exam scheduled — click Bắt đầu — hiển thị ERR_LIVE_EXAM_NOT_OPEN message', async () => {
    const scheduledExam = makeLiveExamItem({ studentStatus: 'not-open', id: 'le-scheduled' });
    await setup([scheduledExam]);
    await initAndLoad();

    // Switch to live-exam tab
    (component as any).onTabChange('live-exam');
    fixture.detectChanges();

    const startButton: HTMLButtonElement = fixture.nativeElement.querySelector('.start-button');
    expect(startButton).toBeTruthy();

    // Click even though disabled (call method directly to test the message logic)
    (component as any).onStartItem(scheduledExam);
    fixture.detectChanges();

    const blocked = fixture.nativeElement.querySelector('.blocked-message');
    expect(blocked).toBeTruthy();
    expect(blocked.textContent).toContain('chưa được mở');
  });

  it('filter theo skill — chỉ hiển thị items khớp skill', async () => {
    const readingItem = makeHomeworkItem({ id: 'hw-r', skill: 'reading', title: 'Reading Hw' });
    const listeningItem = makeHomeworkItem({ id: 'hw-l', skill: 'listening', title: 'Listening Hw' });
    await setup([readingItem, listeningItem]);
    await initAndLoad();

    (component as any).onSkillFilter('reading');
    fixture.detectChanges();

    const filtered = (component as any).filteredHomework();
    expect(filtered.length).toBe(1);
    expect(filtered[0].skill).toBe('reading');
  });

  it('filter theo status — chỉ hiển thị items khớp studentStatus', async () => {
    const availableItem = makeHomeworkItem({ id: 'hw-a', studentStatus: 'available' });
    const expiredItem = makeHomeworkItem({ id: 'hw-e', studentStatus: 'expired' });
    await setup([availableItem, expiredItem]);
    await initAndLoad();

    (component as any).onStatusFilter('available');
    fixture.detectChanges();

    const filtered = (component as any).filteredHomework();
    expect(filtered.length).toBe(1);
    expect(filtered[0].studentStatus).toBe('available');
  });
});
