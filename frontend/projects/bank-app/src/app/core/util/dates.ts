import { IsoDate } from '../../data-access/models/finance.models';

/**
 * The user's local calendar date as "yyyy-MM-dd". `toISOString()` cannot be used: it converts to UTC
 * first, so between 00:00 and 03:00 in Turkey it returns yesterday's date.
 */
export function toIsoDate(date: Date): IsoDate {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function todayIsoDate(): IsoDate {
  return toIsoDate(new Date());
}
