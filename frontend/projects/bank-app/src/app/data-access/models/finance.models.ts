// Mirrors the backend contracts in Bank.Contracts/Finance. Dates travel as ISO "yyyy-MM-dd" strings
// and are kept as strings: converting them to Date objects would shift them by the user's time zone.

export type IsoDate = string;
export type CurrencyCode = 'TRY' | 'USD' | 'EUR';

// --- Credit cards ---

export interface CreditCardRequest {
  name: string;
  limitAmount: number;
  currencyCode: CurrencyCode;
  statementDay: number;
  paymentDueDays: number;
}

export interface CreditCard extends CreditCardRequest {
  id: number;
  /** Row version; sent back in If-Match when updating. */
  version: string;
}

// --- Recurring income, expenses and subscriptions ---

export type CashFlowKind = 'Income' | 'Expense' | 'Subscription';
export type Frequency = 'Weekly' | 'Monthly' | 'Yearly';

export interface RecurringCashFlowRequest {
  name: string;
  kind: CashFlowKind;
  category: string;
  amount: number;
  currencyCode: CurrencyCode;
  frequency: Frequency;
  interval: number;
  startDate: IsoDate;
  endDate: IsoDate | null;
  creditCardId: number | null;
}

export interface RecurringCashFlow extends RecurringCashFlowRequest {
  id: number;
  version: string;
}

// --- Installment plans ---

export interface InstallmentPlanRequest {
  creditCardId: number;
  description: string;
  totalAmount: number;
  installmentCount: number;
  purchaseDate: IsoDate;
}

export interface Installment {
  number: number;
  statementDate: IsoDate;
  dueDate: IsoDate;
  amount: number;
}

export interface InstallmentPlan {
  id: number;
  creditCardId: number;
  description: string;
  totalAmount: number;
  currencyCode: CurrencyCode;
  installmentCount: number;
  purchaseDate: IsoDate;
  installments: Installment[];
}

// --- Cash-flow projection ---

export type ProjectionEntryKind = 'Income' | 'Expense' | 'Subscription' | 'CardPayment';

export interface ProjectionMonth {
  year: number;
  month: number;
  income: number;
  outflows: number;
  cardPayments: number;
  net: number;
  closingBalance: number;
  lowestBalance: number;
  lowestBalanceDate: IsoDate | null;
  burdenRatio: number | null;
  isDeficit: boolean;
  balanceGoesNegative: boolean;
}

export interface CardCharge {
  description: string;
  amount: number;
}

export interface ProjectionEntry {
  date: IsoDate;
  description: string;
  kind: ProjectionEntryKind;
  /** Signed: income positive, outflows negative. */
  amount: number;
  creditCardId: number | null;
  statementDate: IsoDate | null;
  charges: CardCharge[];
}

export interface Projection {
  from: IsoDate;
  to: IsoDate;
  currencyCode: CurrencyCode;
  openingBalance: number;
  months: ProjectionMonth[];
  entries: ProjectionEntry[];
  excludedItems: string[];
}

// --- Installment scenarios ---

export interface PurchaseOptionRequest {
  installmentCount: number;
  totalAmount: number;
}

export interface InstallmentScenarioRequest {
  creditCardId: number;
  description: string;
  purchaseDate: IsoDate;
  options: PurchaseOptionRequest[];
  months: number;
}

export interface ScenarioMetrics {
  lowestBalance: number;
  lowestBalanceDate: IsoDate | null;
  monthsBelowZero: number;
  deficitMonths: number;
  peakBurdenRatio: number | null;
  peakMonthlyOutflow: number;
  closingBalance: number;
  months: ProjectionMonth[];
}

export interface ScenarioOption {
  installmentCount: number;
  totalAmount: number;
  installments: Installment[];
  metrics: ScenarioMetrics;
}

export interface ScenarioComparison {
  from: IsoDate;
  to: IsoDate;
  baseline: ScenarioMetrics;
  options: ScenarioOption[];
}

// --- Transaction history ---

export interface HistoryTransaction {
  id: number;
  date: IsoDate;
  description: string;
  /** Signed: money in positive, money out negative. */
  amount: number;
  currencyCode: CurrencyCode;
  category: string | null;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ImportResult {
  batchId: string;
  totalRows: number;
  importedRows: number;
  duplicateRows: number;
}

// --- Analytics ---

export interface CategoryTrend {
  category: string;
  monthlySpending: number[];
  total: number;
  averageMonthly: number;
  share: number;
  changeFromPreviousMonth: number | null;
  slopePerMonth: number;
}

export interface CategoryTrends {
  /** "yyyy-MM", aligned with every series below. */
  months: string[];
  monthlyTotals: number[];
  categories: CategoryTrend[];
}

export interface RecurringPayment {
  merchant: string;
  direction: 'Inbound' | 'Outbound';
  pattern: 'Fixed' | 'Variable';
  typicalAmount: number;
  typicalDay: number;
  monthsObserved: number;
  lastSeen: IsoDate;
  nextExpected: IsoDate;
  category: string;
  isTracked: boolean;
}

export interface SpendingAnomaly {
  date: IsoDate;
  description: string;
  amount: number;
  category: string;
  typicalAmount: number;
  timesTypical: number;
  comparedWith: string;
  score: number;
}

export interface ForecastMonth {
  month: string;
  expected: number;
  lower: number;
  upper: number;
}

export interface ForecastBacktest {
  method: string;
  forecasts: number;
  meanAbsolutePercentageError: number;
  intervalCoverage: number;
}

export interface SpendingForecast {
  method: string;
  basedOnMonths: number;
  months: ForecastMonth[];
  backtests: ForecastBacktest[];
}
