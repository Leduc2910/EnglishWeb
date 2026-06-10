import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-access-denied',
  imports: [RouterLink],
  template: `
    <main class="access-denied">
      <h1>Không có quyền truy cập</h1>
      <p>Tài khoản hiện tại không thể vào khu vực giáo viên.</p>
      <a routerLink="/login">Quay lại đăng nhập</a>
    </main>
  `,
  styles: `
    .access-denied {
      max-width: 32rem;
      margin: 4rem auto;
      padding: 0 1.5rem;
    }

    .access-denied h1 {
      margin: 0 0 0.75rem;
    }

    .access-denied p {
      margin: 0 0 1rem;
      color: #4b5563;
    }

    .access-denied a:focus-visible {
      outline: 2px solid #166534;
      outline-offset: 2px;
    }
  `,
})
export class AccessDeniedComponent {}
