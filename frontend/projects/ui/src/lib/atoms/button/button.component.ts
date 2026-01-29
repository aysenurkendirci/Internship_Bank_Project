import { Component, Input } from '@angular/core';

@Component({
  selector: 'lib-button',
  standalone: true,
  template: `
    <button [type]="type" class="bank-btn">
      <ng-content></ng-content>
    </button>
  `,
  styles: [`
    .bank-btn {
      width: 100%;
      padding: 14px;
      background: #0052cc;
      color: white;
      border: none;
      border-radius: 10px;
      font-weight: 600;
      cursor: pointer;
      transition: opacity 0.2s;
      &:hover { opacity: 0.9; }
    }
  `]
})
export class ButtonComponent {
  @Input() type: 'button' | 'submit' = 'button';
}