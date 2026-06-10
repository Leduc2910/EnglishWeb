import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TeacherClassesComponent } from './teacher-classes.component';
import { ClassesApiService } from '../../core/classes/classes-api.service';

describe('TeacherClassesComponent', () => {
  let fixture: ComponentFixture<TeacherClassesComponent>;
  let classesApi: {
    getTeacherClasses: ReturnType<typeof vi.fn>;
    getClassDetail: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    classesApi = {
      getTeacherClasses: vi.fn().mockResolvedValue([
        {
          classId: 'class-1',
          className: 'English 7A',
          classCode: 'ENG7A',
          status: 'active',
          enrolledStudentCount: 1,
        },
      ]),
      getClassDetail: vi.fn().mockResolvedValue({
        classId: 'class-1',
        className: 'English 7A',
        classCode: 'ENG7A',
        status: 'active',
        students: [
          {
            studentId: 'student-1',
            displayName: 'student',
            email: 'student@test.local',
            membershipStatus: 'active',
          },
        ],
      }),
    };

    await TestBed.configureTestingModule({
      imports: [TeacherClassesComponent],
      providers: [{ provide: ClassesApiService, useValue: classesApi }],
    }).compileComponents();

    fixture = TestBed.createComponent(TeacherClassesComponent);
    fixture.detectChanges();
  });

  it('renders class name, code, status and student roster', async () => {
    await fixture.componentInstance['loadClasses']();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('English 7A');
    expect(element.textContent).toContain('ENG7A');
    expect(element.textContent).toContain('Đang hoạt động');
    expect(element.textContent).toContain('student@test.local');
  });
});
