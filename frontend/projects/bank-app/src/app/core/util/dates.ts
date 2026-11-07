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

const shortMonth = new Intl.DateTimeFormat('tr-TR', { month: 'short', year: 'numeric' });
const longMonth = new Intl.DateTimeFormat('tr-TR', { month: 'long', year: 'numeric' });

/** "Nis 2026" (short) or "Nisan 2026" (long) for a calendar month given as numbers or as "yyyy-MM". */
export function formatMonth(month: { year: number; month: number } | string, style: 'short' | 'long' = 'short'): string {
  const [year, monthNumber] = typeof month === 'string' ? month.split('-').map(Number) : [month.year, month.month];
  return (style === 'short' ? shortMonth : longMonth).format(new Date(year, monthNumber - 1, 1));
}
