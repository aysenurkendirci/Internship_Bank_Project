import { TransferRequest } from '../../data-access/models/banking.models';

/**
 * Hands out the Idempotency-Key of a transfer. A retry of the same request keeps its key, so a response lost on
 * the way back cannot send the money twice. A changed request is a different transfer and gets a new key, because
 * the server refuses a key that comes back with other details.
 */
export class TransferAttempt {
  private current: { request: string; key: string } | null = null;

  constructor(private readonly newKey: () => string = () => crypto.randomUUID()) {}

  keyFor(request: TransferRequest): string {
    const fingerprint = JSON.stringify(request);
    if (this.current?.request !== fingerprint) this.current = { request: fingerprint, key: this.newKey() };
    return this.current.key;
  }

  /** The transfer went through; the next one is new even if it looks identical. */
  finish(): void {
    this.current = null;
  }
}
