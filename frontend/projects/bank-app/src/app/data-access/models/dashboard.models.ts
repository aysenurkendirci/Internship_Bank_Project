// src/app/data-access/models/dashboard.models.ts
export interface DashboardResponse {
  user: UserSummary;
  totalWealth: number;
  wealthChangeRate: number;
  cards: CardItem[];
  recentTransactions: TransactionItem[]; // Backend'deki IReadOnlyList ile uyumlu
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
  balance: number; // Backend CardItem.Balance property'si
  settings: { contactless: boolean; onlineUse: boolean };
  limits: { dailyLimit: number; monthlyLimit: number };
}

export interface TransactionItem {
  txId: number;
  title: string; // Backend TransactionItem.Title (t.DESCRIPTION'dan maplendi)
  category: string;
  amount: number;
  direction: 'Inbound' | 'Outbound'; // Backend Direction string'i
  createdAt: string;
}