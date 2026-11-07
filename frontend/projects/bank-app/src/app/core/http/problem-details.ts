import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns an API error into a message for the user. The API answers with RFC 9457 problem details:
 * `detail` holds a user-facing sentence, and `errors` is either a field → messages map (request
 * validation) or a list of line messages (rejected CSV import).
 */
export function problemMessage(error: unknown, fallback = 'İşlem tamamlanamadı. Lütfen tekrar deneyin.'): string {
  if (!(error instanceof HttpErrorResponse))
    return fallback;

  if (error.status === 0)
    return 'Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edin.';

  const problem = error.error as { detail?: unknown; errors?: unknown } | null;
  if (typeof problem?.detail === 'string' && problem.detail.length > 0)
    return problem.detail;

  const first = problemErrors(error)[0];
  return first ?? fallback;
}

/** Every individual message in the response, in order: field errors or rejected lines. */
export function problemErrors(error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse))
    return [];

  const errors = (error.error as { errors?: unknown } | null)?.errors;

  if (Array.isArray(errors))
    return errors.filter((e): e is string => typeof e === 'string');

  if (errors && typeof errors === 'object')
    return Object.values(errors as Record<string, unknown>)
      .flatMap((messages) => (Array.isArray(messages) ? messages : []))
      .filter((m): m is string => typeof m === 'string');

  return [];
}
