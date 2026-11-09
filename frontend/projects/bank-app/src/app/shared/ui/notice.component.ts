import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * An error or information message, optionally followed by a list of details (field errors, rejected
 * file lines). Errors are announced to screen readers as alerts, information as status.
 */
@Component({
  selector: 'app-notice',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <div
      [class]="'notice-' + kind()"
      [attr.role]="kind() === 'error' ? 'alert' : 'status'"
    >
      <p>{{ message() }}</p>
      @if (details().length > 0) {
        <ul class="mt-2 list-disc ml-5 space-y-0.5">
          @for (detail of details(); track $index) {
            <li>{{ detail }}</li>
          }
        </ul>
      }
    </div>
  `,
})
export class NoticeComponent {
  readonly message = input.required<string>();
  readonly kind = input<'error' | 'info' | 'success'>('error');
  readonly details = input<readonly string[]>([]);
}
