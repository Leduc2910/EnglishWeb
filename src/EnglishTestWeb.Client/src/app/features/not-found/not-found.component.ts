import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <main class="not-found">
      <h1>Không tìm thấy trang</h1>
      <p>Đường dẫn bạn truy cập không tồn tại.</p>
      <a routerLink="/">Về trang chủ</a>
    </main>
  `,
  styles: `
    .not-found {
      max-width: 32rem;
      margin: 4rem auto;
      padding: 0 1.5rem;
    }

    .not-found p {
      color: #4b5563;
    }
  `,
})
export class NotFoundComponent {}
