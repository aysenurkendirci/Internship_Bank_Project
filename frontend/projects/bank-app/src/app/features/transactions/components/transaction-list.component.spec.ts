import { TestBed } from '@angular/core/testing';

import { TransactionListComponent } from './transaction-list.component';

describe('TransactionListComponent', () => {
  function render(page: number, totalCount: number) {
    const fixture = TestBed.createComponent(TransactionListComponent);
    fixture.componentRef.setInput('items', []);
    fixture.componentRef.setInput('totalCount', totalCount);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', 50);
    fixture.detectChanges();

    const pages: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => pages.push(p));
    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('nav button'));
    return { pages, previous: buttons[0], next: buttons[1], element: fixture.nativeElement as HTMLElement };
  }

  it('hides the pager when everything fits on one page', () => {
    const { element } = render(1, 50);

    expect(element.querySelector('nav')).toBeNull();
  });

  it('cannot go before the first page', () => {
    const { previous, next } = render(1, 120);

    expect(previous.disabled).toBe(true);
    expect(next.disabled).toBe(false);
  });

  it('cannot go past the last page and asks for the previous one', () => {
    const { previous, next, pages } = render(3, 120);

    expect(next.disabled).toBe(true);
    previous.click();
    expect(pages).toEqual([2]);
  });
});
