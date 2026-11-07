import { ProjectionEntry } from '../../data-access/models/finance.models';
import { runningBalance } from './running-balance';

const entry = (date: string, amount: number): ProjectionEntry => ({
  date,
  amount,
  description: 'x',
  kind: amount > 0 ? 'Income' : 'Expense',
  creditCardId: null,
  statementDate: null,
  charges: [],
});

describe('runningBalance', () => {
  it('starts from the opening balance and applies entries in order', () => {
    const points = runningBalance(5000, '2026-04-01', [entry('2026-04-01', -20000), entry('2026-04-15', 45000)]);

    expect(points).toEqual([
      { date: '2026-04-01', balance: -15000 },
      { date: '2026-04-15', balance: 30000 },
    ]);
  });

  it('keeps one point per day with the end-of-day balance', () => {
    const points = runningBalance(0, '2026-04-01', [entry('2026-04-03', -100), entry('2026-04-03', 250)]);

    expect(points).toEqual([
      { date: '2026-04-01', balance: 0 },
      { date: '2026-04-03', balance: 150 },
    ]);
  });

  it('adds cents without floating point drift', () => {
    const points = runningBalance(0.1, '2026-04-01', [entry('2026-04-02', 0.2)]);

    expect(points[1].balance).toBe(0.3);
  });
});
