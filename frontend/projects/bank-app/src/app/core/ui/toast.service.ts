import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
}

const VISIBLE_MS = 4000;

/** Short confirmation that a change went through. Errors stay on the page, next to what failed. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toastsSignal = signal<readonly Toast[]>([]);
  private nextId = 1;

  readonly toasts = this.toastsSignal.asReadonly();

  show(message: string): void {
    const id = this.nextId++;
    this.toastsSignal.update((toasts) => [...toasts, { id, message }]);
    setTimeout(() => this.toastsSignal.update((toasts) => toasts.filter((t) => t.id !== id)), VISIBLE_MS);
  }
}
