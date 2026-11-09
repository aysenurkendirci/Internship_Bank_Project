import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { BasePage } from '../../core/page/base-page';
import { formatIban, isValidIban } from '../../core/util/iban';
import { AccountsApi, TransfersApi } from '../../data-access/api/banking.api';
import { Account, TransferReceipt, TransferRequest } from '../../data-access/models/banking.models';
import { ACCOUNT_TYPE_DISPLAY } from '../../shared/finance/account-display';
import { NoticeComponent } from '../../shared/ui/notice.component';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { TransferAttempt } from './transfer-attempt';

type Step = 'form' | 'review' | 'receipt';

const ibanValidator = (control: AbstractControl<string>): ValidationErrors | null =>
  isValidIban(control.value) ? null : { iban: true };

/** Sending money in three steps: details, a last look, then the receipt. Money moves only on the second step. */
@Component({
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, NoticeComponent, PageHeaderComponent, SkeletonComponent],
  templateUrl: './transfer-page.component.html',
})
export class TransferPageComponent extends BasePage implements OnInit {
  /** From the link: ?from=<account id>, to start with that account selected. */
  readonly from = input<string>();

  private readonly accountsApi = inject(AccountsApi);
  private readonly transfers = inject(TransfersApi);
  private readonly attempt = new TransferAttempt();

  protected readonly typeDisplay = ACCOUNT_TYPE_DISPLAY;
  protected readonly printed = formatIban;

  protected readonly step = signal<Step>('form');
  protected readonly accounts = signal<readonly Account[]>([]);
  protected readonly receipt = signal<TransferReceipt | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    sourceAccountId: [0, Validators.min(1)],
    targetIban: ['', [Validators.required, ibanValidator]],
    recipientName: ['', [Validators.required, Validators.maxLength(140)]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    description: ['', [Validators.required, Validators.maxLength(140)]],
  });

  ngOnInit(): void {
    this.load(
      this.accountsApi.list(),
      (accounts) => {
        const usable = accounts.filter((a) => a.status === 'Active');
        this.accounts.set(usable);
        const requested = usable.find((a) => a.id === Number(this.from()));
        this.form.controls.sourceAccountId.setValue((requested ?? usable[0])?.id ?? 0);
      },
      'Hesaplar yüklenemedi.',
    );
  }

  protected source(): Account | undefined {
    return this.accounts().find((a) => a.id === Number(this.form.controls.sourceAccountId.value));
  }

  protected exceedsBalance(): boolean {
    const amount = this.form.controls.amount.value;
    const source = this.source();
    return amount !== null && source !== undefined && amount > source.balance;
  }

  protected show(field: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[field];
    return control.touched && control.invalid;
  }

  /** Groups the IBAN in fours while it is typed or pasted, the way it is printed on a statement. */
  protected groupIban(): void {
    const control = this.form.controls.targetIban;
    control.setValue(formatIban(control.value.toUpperCase()), { emitEvent: false });
  }

  protected sendAll(): void {
    this.form.controls.amount.setValue(this.source()?.balance ?? null);
  }

  protected review(): void {
    this.form.markAllAsTouched();
    this.error.set(null);
    if (this.form.valid && !this.exceedsBalance()) this.step.set('review');
  }

  protected edit(): void {
    this.error.set(null);
    this.step.set('form');
  }

  /** After a failure the same button retries, with the same key, so a lost response is safe to repeat. */
  protected confirm(): void {
    const request = this.request();

    this.act(this.transfers.send(request, this.attempt.keyFor(request)), {
      fallback: 'Transfer tamamlanamadı.',
      onDone: (receipt) => {
        this.attempt.finish();
        this.receipt.set(receipt);
        this.step.set('receipt');
      },
    });
  }

  protected startOver(): void {
    this.form.reset({ sourceAccountId: this.form.controls.sourceAccountId.value });
    this.receipt.set(null);
    this.step.set('form');
    // The balance on the account just used has changed.
    this.ngOnInit();
  }

  protected print(): void {
    window.print();
  }

  protected request(): TransferRequest {
    const value = this.form.getRawValue();
    return {
      sourceAccountId: Number(value.sourceAccountId),
      targetIban: value.targetIban.replace(/\s+/g, ''),
      recipientName: value.recipientName.trim(),
      amount: value.amount ?? 0,
      description: value.description.trim(),
    };
  }
}
