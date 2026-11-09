import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  title: string;
  message: string;
  confirmLabel: string;
  danger?: boolean;
}

/** Asks the user a yes/no question from anywhere; the layout renders the dialog. */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly requestSignal = signal<ConfirmRequest | null>(null);
  private resolve: ((answer: boolean) => void) | null = null;

  readonly request = this.requestSignal.asReadonly();

  ask(request: ConfirmRequest): Promise<boolean> {
    this.resolve?.(false);
    this.requestSignal.set(request);
    return new Promise((resolve) => (this.resolve = resolve));
  }

  confirmDelete(message: string): Promise<boolean> {
    return this.ask({ title: 'Silme onayı', message, confirmLabel: 'Sil', danger: true });
  }

  answer(answer: boolean): void {
    this.resolve?.(answer);
    this.resolve = null;
    this.requestSignal.set(null);
  }
}
