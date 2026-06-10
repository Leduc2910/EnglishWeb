import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { TeacherShellComponent } from './teacher-shell.component';
import { AuthSessionService } from '../../../core/auth/auth-session.service';

describe('TeacherShellComponent', () => {
  let fixture: ComponentFixture<TeacherShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeacherShellComponent],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            currentUser: signal({
              userId: '1',
              email: 'teacher@test.local',
              userName: 'teacher',
              roles: ['Teacher'],
            }),
            logout: vi.fn().mockResolvedValue(undefined),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TeacherShellComponent);
    fixture.detectChanges();
  });

  it('renders teacher navigation labels', () => {
    const text = fixture.nativeElement.textContent ?? '';
    expect(text).toContain('Dashboard');
    expect(text).toContain('Thư viện đề');
    expect(text).toContain('Lớp học');
    expect(text).toContain('Kết quả');
  });
});
