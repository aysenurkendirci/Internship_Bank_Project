import { Component, DestroyRef, ElementRef, afterNextRender, effect, inject, input, viewChild } from '@angular/core';
import { BarChart, LineChart } from 'echarts/charts';
import { GridComponent, LegendComponent, MarkLineComponent, TooltipComponent } from 'echarts/components';
import { ECharts, EChartsCoreOption, init, use } from 'echarts/core';
import { SVGRenderer } from 'echarts/renderers';

// Only the pieces the app draws are bundled; importing 'echarts' whole would add every chart type.
use([LineChart, BarChart, GridComponent, TooltipComponent, LegendComponent, MarkLineComponent, SVGRenderer]);

/**
 * Thin wrapper around ECharts: creates the chart after the first render (it needs a sized element),
 * re-applies the option whenever the input changes, follows the container's size and disposes itself.
 */
@Component({
  selector: 'app-chart',
  template: `<div #host class="w-full h-full" role="img" [attr.aria-label]="label()"></div>`,
  host: { class: 'block' },
})
export class ChartComponent {
  readonly option = input.required<EChartsCoreOption>();
  /** Accessible summary; the page also offers the same data as a table. */
  readonly label = input.required<string>();

  private readonly host = viewChild.required<ElementRef<HTMLDivElement>>('host');
  private chart: ECharts | undefined;

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      const element = this.host().nativeElement;
      const chart = init(element, null, { renderer: 'svg' });
      chart.setOption(this.option());
      this.chart = chart;

      const resize = new ResizeObserver(() => chart.resize());
      resize.observe(element);

      destroyRef.onDestroy(() => {
        resize.disconnect();
        chart.dispose();
      });
    });

    effect(() => {
      const option = this.option();
      this.chart?.setOption(option, { notMerge: true });
    });
  }
}
