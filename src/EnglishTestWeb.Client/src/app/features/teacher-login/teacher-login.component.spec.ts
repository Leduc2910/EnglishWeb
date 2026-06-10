import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TeacherLoginComponent } from './teacher-login.component';
import { AuthSessionService } from '../../core/auth/auth-session.service';

describe('TeacherLoginComponent', () => {
  let fixture: ComponentFixture<TeacherLoginComponent>;
  let auth: { login: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    auth = {
      login: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [TeacherLoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: auth },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TeacherLoginComponent);
    fixture.detectChanges();
  });

  it('shows identifier required error on empty submit', async () => {
    await fixture.componentInstance['onSubmit']();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('#teacher-login-error-alert');
    expect(alert).toBeTruthy();
    expect(alert?.getAttribute('data-error-code')).toBe('ERR_LOGIN_IDENTIFIER_REQUIRED');
    expect(alert?.textContent).toContain('Nhập email hoặc tên đăng nhập');
  });

  it('shows password required error when identifier is filled', async () => {
    fixture.componentInstance['form'].controls.identifier.setValue('teacher@test.local');
    await fixture.componentInstance['onSubmit']();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('#teacher-login-error-alert');
    expect(alert?.getAttribute('data-error-code')).toBe('ERR_LOGIN_PASSWORD_REQUIRED');
  });
});
