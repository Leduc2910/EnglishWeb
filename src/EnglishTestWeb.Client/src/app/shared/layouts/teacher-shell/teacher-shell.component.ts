import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthSessionService } from '../../../core/auth/auth-session.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-teacher-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './teacher-shell.component.html',
  styleUrl: './teacher-shell.component.css',
})
export class TeacherShellComponent {
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly currentUser = this.auth.currentUser;

  protected async logout(): Promise<void> {
    await this.auth.logout();
    await this.router.navigateByUrl('/login');
  }
}
