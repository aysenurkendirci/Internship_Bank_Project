// Accounts, transfers, issued cards and the customer profile; these mirror the matching Bank.Contracts records.
import { AccountStatus, AccountType } from './dashboard.models';

export type AccountCurrency = 'TRY' | 'USD' | 'EUR';

export interface Account {
  id: number;
  iban: string;
  accountType: AccountType;
  currencyCode: string;
  balance: number;
  status: AccountStatus;
}

export interface StatementEntry {
  entryId: number;
  transferId: number;
  side: 'Debit' | 'Credit';
  amount: number;
  currencyCode: string;
  balanceAfter: number;
  description: string;
  counterpartyIban: string;
  createdAt: string;
}

export interface TransferRequest {
  sourceAccountId: number;
  targetIban: string;
  recipientName: string;
  amount: number;
  description: string;
}

export interface TransferReceipt {
  id: number;
  sourceAccountId: number;
  sourceIban: string;
  targetIban: string;
  recipientName: string;
  amount: number;
  currencyCode: string;
  description: string;
  sourceBalanceAfter: number;
  createdAt: string;
}

export interface CardSettingsRequest {
  contactless: boolean;
  onlineUse: boolean;
  dailyLimit: number;
  monthlyLimit: number;
}

export interface CardSettingsResponse extends CardSettingsRequest {
  id: number;
  status: string;
}

export interface Profile {
  firstName: string;
  lastName: string;
  maskedTcNo: string;
  email: string;
  phone: string;
  membership: string;
  registeredAt: string;
  lastLoginAt: string | null;
}

export interface ContactDetails {
  email: string;
  phone: string;
}

export interface PasswordChange {
  currentPassword: string;
  newPassword: string;
}
