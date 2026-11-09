import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Placeholder bars shown while a page's first data is on its way. */
@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block', role: 'status', 'aria-label': 'Yükleniyor' },
  template: `
    <div class="space-y-3 animate-pulse">
      @for (row of bars(); track row) {
        <div class="h-16 rounded-xl bg-line"></div>
      }
    </div>
  `,
})
export class SkeletonComponent {
  readonly rows = input(3);

  protected readonly bars = computed(() => Array.from({ length: this.rows() }, (_, i) => i));
}
