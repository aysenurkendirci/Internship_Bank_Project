export interface DashboardResponse {
  user: UserSummary;
  totalWealth: number;
  wealthChangeRate: number;
  cards: CardItem[];
  recentTransactions: TransactionItem[];
  accounts: AccountItem[]; // Tasarımdaki sağ sütun için gerekli
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

export interface AccountItem {
  accountId: number;
  accountName: string;
  accountNo: string;
  balance: number;
  status: string;
  subtitle?: string; // Örn: "%32 Faiz" veya "Aktif"
  iconType: 'bank' | 'trend' | 'wallet';
}