import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ClassContextService } from '../../core/classes/class-context.service';

@Component({
  selector: 'app-student-assigned-tests',
  imports: [RouterLink],
  templateUrl: './student-assigned-tests.component.html',
  styleUrl: './student-assigned-tests.component.css',
})
export class StudentAssignedTestsComponent {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthSessionService);
  protected readonly classContext = inject(ClassContextService);

  protected async logout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigate(['/class']);
  }
}
