import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { StudentLoginComponent } from './student-login.component';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ClassContextService } from '../../core/classes/class-context.service';
import { ClassesApiService } from '../../core/classes/classes-api.service';

describe('StudentLoginComponent', () => {
  let fixture: ComponentFixture<StudentLoginComponent>;
  let auth: {
    loginStudent: ReturnType<typeof vi.fn>;
    mapStudentApiError: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
  };
  let classContext: {
    confirmedClass: ReturnType<typeof vi.fn>;
    readPersistedClassCode: ReturnType<typeof vi.fn>;
    isConfirmedForClass: ReturnType<typeof vi.fn>;
    clearClassContext: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    auth = {
      loginStudent: vi.fn(),
      mapStudentApiError: vi.fn().mockReturnValue('Tài khoản hoặc mật khẩu chưa đúng.'),
      isAuthenticated: vi.fn().mockReturnValue(false),
    };
    classContext = {
      confirmedClass: vi.fn().mockReturnValue({
        className: 'English 7A',
        teacherDisplayName: 'Teacher',
      }),
      readPersistedClassCode: vi.fn().mockReturnValue('ENG7A'),
      isConfirmedForClass: vi.fn().mockReturnValue(true),
      clearClassContext: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StudentLoginComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => 'ENG7A' } } },
        },
        { provide: AuthSessionService, useValue: auth },
        { provide: ClassContextService, useValue: classContext },
        {
          provide: ClassesApiService,
          useValue: { lookupByCode: vi.fn() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentLoginComponent);
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('shows identifier required error on empty submit', async () => {
    await fixture.componentInstance['onSubmit']();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('#student-login-error-alert');
    expect(alert?.getAttribute('data-error-code')).toBe('ERR_STUDENT_IDENTIFIER_REQUIRED');
  });

  it('maps login failure to generic invalid credentials message', async () => {
    auth.loginStudent.mockRejectedValue(new Error('401'));
    auth.mapStudentApiError.mockReturnValue('Tài khoản hoặc mật khẩu chưa đúng.');

    fixture.componentInstance['form'].setValue({
      identifier: 'student@test.local',
      password: 'Student123!',
    });
    fixture.componentInstance['classCode'].set('ENG7A');

    await fixture.componentInstance['onSubmit']();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('#student-login-error-alert');
    expect(alert?.getAttribute('data-error-code')).toBe('ERR_STUDENT_LOGIN_INVALID');
    expect(alert?.textContent).toContain('Tài khoản hoặc mật khẩu chưa đúng.');
  });
});
