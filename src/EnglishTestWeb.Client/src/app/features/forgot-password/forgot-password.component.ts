import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  imports: [RouterLink],
  template: `
    <main class="forgot-password">
      <h1>Quên mật khẩu</h1>
      <p>Luồng khôi phục mật khẩu sẽ được bổ sung sau MVP baseline.</p>
      <a routerLink="/login">Quay lại đăng nhập</a>
    </main>
  `,
  styles: `
    .forgot-password {
      max-width: 32rem;
      margin: 4rem auto;
      padding: 0 1.5rem;
    }

    .forgot-password p {
      color: #4b5563;
      line-height: 1.5;
    }
  `,
})
export class ForgotPasswordComponent {}
