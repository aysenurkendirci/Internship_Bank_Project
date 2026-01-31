import { Component, Input } from '@angular/core';

@Component({
  selector: 'lib-button',
  standalone: true,
  template: `
    <button class="btn" [type]="type" [disabled]="disabled">
      <ng-content></ng-content>
    </button>
  `,
  styles: [`
    .btn{
      width:100%;
      padding:14px 16px;
      border-radius:16px;
      border:0;
      font-weight:900;
      cursor:pointer;
      transition:.2s;
      background: linear-gradient(90deg,#2563eb,#06b6d4);
      color:#fff;
      opacity: 1;
    }
    .btn:disabled{ opacity:.6; cursor:not-allowed; }
  `]
})
export class ButtonComponent {
  @Input() type: 'button' | 'submit' = 'button';
  @Input() disabled = false;
}
