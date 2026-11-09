import { Directive, input, output } from '@angular/core';
import { AbstractControl } from '@angular/forms';

/**
 * Base of add/edit forms. A form only collects input: it emits a finished request and the page decides
 * what to do with it, so the same form works for creating and for editing.
 */
@Directive()
export abstract class BaseForm<TRequest> {
  /** Set by the page while saving. */
  readonly busy = input(false);
  readonly save = output<TRequest>();
  readonly cancel = output<void>();

  protected abstract readonly form: AbstractControl;

  /** Turns the form's raw values into what the API expects. */
  protected abstract toRequest(): TRequest;

  /** Touching every field first makes each invalid one show its message. */
  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.valid) this.save.emit(this.toRequest());
  }
}
