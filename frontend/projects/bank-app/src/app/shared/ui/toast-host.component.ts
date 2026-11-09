import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ToastService } from '../../core/ui/toast.service';

/** Where confirmations appear. Screen readers announce them through the live region. */
@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'fixed bottom-4 right-4 left-4 sm:left-auto z-20 space-y-2 print:hidden', 'aria-live': 'polite' },
  template: `
    @for (toast of service.toasts(); track toast.id) {
      <p class="rounded-lg bg-brand text-white text-sm px-4 py-3">{{ toast.message }}</p>
    }
  `,
})
export class ToastHostComponent {
  protected readonly service = inject(ToastService);
}
