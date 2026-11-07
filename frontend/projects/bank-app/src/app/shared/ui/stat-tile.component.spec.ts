import { TestBed } from '@angular/core/testing';

import { StatTileComponent } from './stat-tile.component';

describe('StatTileComponent', () => {
  function render(critical: boolean): HTMLElement {
    const fixture = TestBed.createComponent(StatTileComponent);
    fixture.componentRef.setInput('label', 'En düşük bakiye');
    fixture.componentRef.setInput('value', '−15.000 ₺');
    fixture.componentRef.setInput('critical', critical);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('marks a critical value with an icon, not colour alone', () => {
    expect(render(true).querySelector('.material-symbols-outlined')?.textContent).toBe('warning');
  });

  it('shows no warning icon for a normal value', () => {
    expect(render(false).querySelector('.material-symbols-outlined')).toBeNull();
  });
});
