import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Shown instead of an empty list; the content says what to do next. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `<p class="panel text-slate-500 font-semibold"><ng-content /></p>`,
})
export class EmptyStateComponent {}
