export interface DashboardResponse {
  user: { userId: number; firstName: string; membership: string };
  totalWealth: number;
  wealthChangeRate: number;
  cards: CardItem[];
  recentTransactions: TransactionItem[];
}

export interface CardItem {
  cardId: number;
  cardNoMasked: string;
  cardType: string;
  isVirtual: boolean;
  status: string;
  balance: number;
  settings: { contactless: boolean; onlineUse: boolean };
  limits: { dailyLimit: number; monthlyLimit: number };
}

export interface TransactionItem {
  txId: number;
  title: string;
  category: string;
  amount: number;
  direction: 'Inbound' | 'Outbound';
  createdAt: string;
}
