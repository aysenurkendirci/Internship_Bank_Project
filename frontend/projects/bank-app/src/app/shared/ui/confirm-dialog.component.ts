import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, viewChild } from '@angular/core';

import { ConfirmService } from '../../core/ui/confirm.service';

/** Renders ConfirmService's question. The native dialog gives focus trapping and Escape for free. */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog #dialog class="m-auto w-full max-w-sm rounded-xl p-5 bg-surface text-ink backdrop:bg-ink/40" (close)="confirm.answer(false)">
      @if (confirm.request(); as request) {
        <h2 class="section-title">{{ request.title }}</h2>
        <p class="mt-2 text-sm text-ink-soft">{{ request.message }}</p>
        <div class="mt-5 flex justify-end gap-2">
          <button type="button" class="btn-secondary" (click)="confirm.answer(false)">Vazgeç</button>
          <button type="button" [class]="request.danger ? 'btn-danger' : 'btn-primary'" (click)="confirm.answer(true)">
            {{ request.confirmLabel }}
          </button>
        </div>
      }
    </dialog>
  `,
})
export class ConfirmDialogComponent {
  protected readonly confirm = inject(ConfirmService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const element = this.dialog().nativeElement;
      if (this.confirm.request()) {
        if (!element.open) element.showModal();
      } else if (element.open) {
        element.close();
      }
    });
  }
}
