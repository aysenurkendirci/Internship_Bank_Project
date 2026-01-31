import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';

@Component({
  selector: 'lib-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="field">
      <label *ngIf="label">{{ label }}</label>
      <input class="inp" [type]="type" [placeholder]="placeholder" [formControl]="control" />
      <small class="err" *ngIf="showError">{{ errorText }}</small>
    </div>
  `,
  styles: [`
    .field{ display:flex; flex-direction:column; gap:6px; }
    label{ font-size:12px; font-weight:800; color:#64748b; }
    .inp{
      padding:14px 14px;
      border-radius:14px;
      border:1px solid rgba(148,163,184,.25);
      outline:none;
      transition:.2s;
    }
    .inp:focus{
      border-color: rgba(37,99,235,.55);
      box-shadow: 0 0 0 4px rgba(37,99,235,.12);
    }
    .err{ color:#ef4444; font-weight:700; }
  `]
})
export class InputComponent {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() type: 'text' | 'password' | 'email' | 'tel' = 'text';
  @Input() control = new FormControl();
  @Input() showError = false;
  @Input() errorText = 'Bu alan zorunlu';
}
