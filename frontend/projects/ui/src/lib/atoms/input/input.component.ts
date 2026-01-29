import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';

@Component({
  selector: 'lib-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="field-container">
      <label *ngIf="label">{{ label }}</label>
      <input 
        [type]="type" 
        [placeholder]="placeholder" 
        [formControl]="control"
        class="bank-input"
      />
    </div>
  `,
  styles: [`
    .field-container { margin-bottom: 1.2rem; display: flex; flex-direction: column; }
    label { font-size: 0.85rem; font-weight: 600; color: #333; margin-bottom: 0.4rem; }
    .bank-input {
      padding: 12px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      font-size: 0.95rem;
      transition: border 0.2s;
      &:focus { border-color: #0052cc; outline: none; }
    }
  `]
})
export class InputComponent {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() type = 'text';
  @Input() control = new FormControl();
}