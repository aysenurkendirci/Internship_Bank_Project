import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

import { formatIban } from '../../core/util/iban';

/** An IBAN in its printed form. Clicking copies it without the spaces, ready to paste. */
@Component({
  selector: 'app-iban-copy',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button type="button" (click)="copy()" class="text-xs text-ink-faint tabular-nums hover:text-ink text-left" title="IBAN'ı kopyala">
      {{ copied() ? 'Kopyalandı' : printed(iban()) }}
    </button>
  `,
})
export class IbanCopyComponent {
  readonly iban = input.required<string>();

  protected readonly copied = signal(false);
  protected readonly printed = formatIban;

  protected copy(): void {
    navigator.clipboard.writeText(this.iban()).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 1500);
    });
  }
}
