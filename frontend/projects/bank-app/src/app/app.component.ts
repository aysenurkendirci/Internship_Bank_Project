import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** The root of the app; everything visible lives under the router. */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <router-outlet></router-outlet>
  `,
})
export class AppComponent {}
