import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { StudentClassEntryComponent } from './student-class-entry.component';
import { ClassesApiService } from '../../core/classes/classes-api.service';
import { ClassContextService } from '../../core/classes/class-context.service';

describe('StudentClassEntryComponent', () => {
  let fixture: ComponentFixture<StudentClassEntryComponent>;
  let classesApi: { lookupByCode: ReturnType<typeof vi.fn> };
  let classContext: { setConfirmedClass: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(async () => {
    classesApi = {
      lookupByCode: vi.fn(),
    };
    classContext = {
      setConfirmedClass: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StudentClassEntryComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => null } } },
        },
        { provide: ClassesApiService, useValue: classesApi },
        { provide: ClassContextService, useValue: classContext },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentClassEntryComponent);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
  });

  it('shows required error on empty submit', async () => {
    await fixture.componentInstance['onLookup']();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('#student-class-entry-error-alert');
    expect(alert?.getAttribute('data-error-code')).toBe('ERR_CLASS_CODE_REQUIRED');
  });

  it('navigates to student login after confirm', async () => {
    classesApi.lookupByCode.mockResolvedValue({
      classId: 'id-1',
      className: 'English 7A',
      classCode: 'ENG7A',
      teacherDisplayName: 'Teacher',
      status: 'active',
    });

    fixture.componentInstance['form'].controls.classCode.setValue('ENG7A');
    await fixture.componentInstance['onLookup']();
    fixture.detectChanges();
    await fixture.componentInstance['confirmClass']();

    expect(classContext.setConfirmedClass).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/student/login'], {
      queryParams: { classCode: 'ENG7A' },
    });
  });
});
