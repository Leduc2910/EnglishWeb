import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-teacher-placeholder',
  template: `
    <section class="placeholder-page">
      <h1>{{ title() }}</h1>
      <p>{{ description() }}</p>
    </section>
  `,
  styles: `
    .placeholder-page h1 {
      margin: 0 0 0.5rem;
      font-size: 1.5rem;
    }

    .placeholder-page p {
      margin: 0;
      color: #4b5563;
      line-height: 1.5;
    }
  `,
})
export class TeacherPlaceholderComponent {
  private readonly route = inject(ActivatedRoute);

  protected readonly title = toSignal(
    this.route.data.pipe(map((data) => String(data['title'] ?? 'Module'))),
    { initialValue: 'Module' },
  );

  protected readonly description = toSignal(
    this.route.data.pipe(
      map(
        (data) =>
          String(
            data['description'] ??
              'Module này sẽ được triển khai trong story tiếp theo.',
          ),
      ),
    ),
    {
      initialValue: 'Module này sẽ được triển khai trong story tiếp theo.',
    },
  );
}
