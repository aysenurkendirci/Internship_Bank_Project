import { CashFlowKind, Frequency } from '../../data-access/models/finance.models';

export const KIND_LABELS: Record<CashFlowKind, string> = {
  Income: 'Gelir',
  Expense: 'Gider',
  Subscription: 'Abonelik',
};

const FREQUENCY_UNITS: Record<Frequency, string> = {
  Weekly: 'hafta',
  Monthly: 'ay',
  Yearly: 'yıl',
};

/** "Her ay", "3 ayda bir", "2 haftada bir". The locative suffix -da fits all three units. */
export function describeSchedule(frequency: Frequency, interval: number): string {
  const unit = FREQUENCY_UNITS[frequency];
  return interval === 1 ? `Her ${unit}` : `${interval} ${unit}da bir`;
}
