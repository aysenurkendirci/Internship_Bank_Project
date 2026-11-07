export interface DashboardResponse {
  user: UserSummary;
  totalWealth: number;
  cards: CardItem[];
  recentTransactions: TransactionItem[];
  accounts: AccountItem[];
}

export interface UserSummary {
  userId: number;
  firstName: string;
  membership: string;
}

export interface CardItem {
  cardId: number;
  cardNoMasked: string;
  cardType: string;
  isVirtual: boolean;
  status: string;
  balance: number;
  settings?: { contactless: boolean; onlineUse: boolean };
  limits?: { dailyLimit: number; monthlyLimit: number };
}

export interface TransactionItem {
  txId: number;
  title: string;
  category: string;
  amount: number;
  direction: 'Inbound' | 'Outbound';
  createdAt: string;
}

export type AccountType = 'Demand' | 'Time' | 'ForeignCurrency';
export type AccountStatus = 'Active' | 'Frozen' | 'Closed';

export interface AccountItem {
  accountId: number;
  accountType: AccountType;
  currencyCode: string;
  iban: string;
  balance: number;
  status: AccountStatus;
}