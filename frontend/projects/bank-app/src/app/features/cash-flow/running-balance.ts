import { IsoDate, ProjectionEntry } from '../../data-access/models/finance.models';

export interface BalancePoint {
  date: IsoDate;
  balance: number;
}

/**
 * The account balance after each day of the projection, starting from the opening balance.
 * Entries arrive ordered by the API (same-day outflows first), so applying them in order reproduces
 * the lowest balance the API reports. One point per day: the balance at the end of that day.
 */
export function runningBalance(openingBalance: number, from: IsoDate, entries: readonly ProjectionEntry[]): BalancePoint[] {
  const points: BalancePoint[] = [{ date: from, balance: openingBalance }];
  let balance = openingBalance;

  for (const entry of entries) {
    // Cents are added as integers so long sums do not drift (0.1 + 0.2 !== 0.3 in floating point).
    balance = Math.round(balance * 100 + entry.amount * 100) / 100;

    const last = points[points.length - 1];
    if (last.date === entry.date) last.balance = balance;
    else points.push({ date: entry.date, balance });
  }

  return points;
}
