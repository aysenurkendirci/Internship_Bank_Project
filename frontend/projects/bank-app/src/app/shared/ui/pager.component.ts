import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Previous/next paging. Renders nothing when everything fits on one page. */
@Component({
  selector: 'app-pager',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (pageCount() > 1) {
      <nav class="flex items-center justify-center gap-4 mt-6" aria-label="Sayfalar">
        <button type="button" class="btn-secondary" (click)="pageChange.emit(page() - 1)" [disabled]="page() === 1">Önceki</button>
        <span class="text-sm text-ink-soft tabular-nums">{{ page() }} / {{ pageCount() }}</span>
        <button type="button" class="btn-secondary" (click)="pageChange.emit(page() + 1)" [disabled]="page() === pageCount()">Sonraki</button>
      </nav>
    }
  `,
})
export class PagerComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
}
