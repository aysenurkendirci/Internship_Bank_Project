import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Directive, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import { problemMessage } from '../http/problem-details';

export interface ActOptions<T> {
  onDone?: (result: T) => void;
  /** Shown when the API sends no message of its own. */
  fallback?: string;
  onError?: (error: unknown) => void;
  /** The record changed underneath the user (409), so what is on screen is out of date. */
  onConflict?: () => void;
}

/**
 * Base of every page (container) component: the loading, saving and error state each of them needs.
 *
 * Pages own this state and pass plain values down; display components never call an API. Requests are
 * cancelled when the user leaves the page, so a late response cannot touch a destroyed component.
 */
@Directive()
export abstract class BasePage {
  private readonly destroyRef = inject(DestroyRef);

  /** True while the page's data is being fetched. */
  protected readonly loading = signal(false);
  /** True while a change is being saved; used to disable buttons against double submits. */
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  /** Fetches data for display. */
  protected load<T>(source: Observable<T>, onData: (data: T) => void, fallback: string): void {
    this.loading.set(true);
    this.error.set(null);

    source.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        onData(data);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(problemMessage(err, fallback));
        this.loading.set(false);
      },
    });
  }

  /** Runs a change the user asked for. */
  protected act<T>(action: Observable<T>, options: ActOptions<T> = {}): void {
    this.busy.set(true);
    this.error.set(null);

    action.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.busy.set(false);
        options.onDone?.(result);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.error.set(problemMessage(err, options.fallback));
        options.onError?.(err);
        if (err instanceof HttpErrorResponse && err.status === 409) options.onConflict?.();
      },
    });
  }
}
