import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { Observable } from 'rxjs';

export type CardDetailResponse = {
  cardId: number;
  cardNo: string;
  cardType: string;
  isVirtual: boolean;
  status: string;
  accountId: number;
  accountType: string;
  iban: string;
  accountBalance: number;
  contactless: boolean;
  onlineUse: boolean;
  dailyLimit: number;
  monthlyLimit: number;
  ownerFirstName: string;
  membership: string;
};

@Injectable({ providedIn: 'root' })
export class CardsApi {
  private http = inject(HttpClient);
  private base = inject(API_BASE_URL);

  getById(id: number): Observable<CardDetailResponse> {
    return this.http.get<CardDetailResponse>(`${this.base}/api/cards/${id}`);
  }
}
