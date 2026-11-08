import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A single headline figure. The value arrives already formatted (currency, count), so the tile stays
 * unaware of units. A critical tile also shows a warning icon: the state is never carried by colour alone.
 */
@Component({
  selector: 'app-stat-tile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'panel block' },
  template: `
    <p class="text-sm text-ink-soft">{{ label() }}</p>
    <p class="text-2xl font-semibold tabular-nums mt-2 flex items-center gap-1.5" [class]="critical() ? 'text-red-700' : 'text-ink'">
      @if (critical()) {
        <span class="material-symbols-outlined text-[22px]" aria-hidden="true">warning</span>
      }
      {{ value() }}
    </p>
    @if (hint()) {
      <p class="text-xs text-ink-faint mt-1">{{ hint() }}</p>
    }
  `,
})
export class StatTileComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly critical = input(false);
}
