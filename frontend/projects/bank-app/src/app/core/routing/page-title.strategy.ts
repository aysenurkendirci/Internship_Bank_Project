import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

/** Names the browser tab after the page, from the `title` of its route. */
@Injectable()
export class PageTitleStrategy extends TitleStrategy {
  private readonly browserTitle = inject(Title);

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const page = this.buildTitle(snapshot);
    this.browserTitle.setTitle(page ? `${page} | NeoBank` : 'NeoBank');
  }
}
