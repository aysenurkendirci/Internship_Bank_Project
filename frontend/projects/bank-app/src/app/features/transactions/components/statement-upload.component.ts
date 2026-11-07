import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { ImportResult } from '../../../data-access/models/finance.models';
import { NoticeComponent } from '../../../shared/ui/notice.component';

const MAX_FILE_BYTES = 2 * 1024 * 1024;

/** File picker and demo button, plus the outcome of the last import. */
@Component({
  selector: 'app-statement-upload',
  imports: [NoticeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'panel block space-y-4' },
  template: `
    <div class="flex flex-wrap items-center gap-3">
      <label class="btn-primary cursor-pointer" [class.opacity-50]="busy()">
        <span class="material-symbols-outlined">upload_file</span> CSV ekstre yükle
        <input type="file" accept=".csv,text/csv" class="sr-only" [disabled]="busy()" (change)="pick($any($event.target))" />
      </label>
      <button type="button" class="btn-secondary" (click)="demo.emit()" [disabled]="busy()">
        <span class="material-symbols-outlined">science</span> 12 aylık demo verisi oluştur
      </button>
      @if (busy()) {
        <span class="text-sm font-bold text-slate-500">İşleniyor...</span>
      }
    </div>

    <p class="text-xs font-semibold text-slate-400">
      Gerekli kolonlar: Tarih, Açıklama ve Tutar (ya da Borç ve Alacak). Kategori isteğe bağlı. Noktalı virgül veya virgülle ayrılmış,
      UTF-8 ya da Excel'in Türkçe kodlamasıyla kaydedilmiş dosyalar okunur. Aynı satır ikinci kez eklenmez.
    </p>

    @if (result(); as r) {
      <app-notice kind="info" [message]="summary(r)" />
    }
    @if (error(); as message) {
      <app-notice [message]="message" [details]="details()" />
    }
  `,
})
export class StatementUploadComponent {
  readonly busy = input(false);
  readonly result = input<ImportResult | null>(null);
  readonly error = input<string | null>(null);
  readonly details = input<readonly string[]>([]);

  readonly file = output<File>();
  readonly demo = output<void>();
  /** Raised for problems found before anything is sent, such as a file over the size limit. */
  readonly rejected = output<string>();

  protected pick(input: HTMLInputElement): void {
    const file = input.files?.[0];
    input.value = ''; // lets the user pick the same file again after fixing it
    if (!file) return;

    // The API refuses large files too; checking here saves the user from waiting for the upload.
    if (file.size > MAX_FILE_BYTES) this.rejected.emit('Dosya en fazla 2 MB olabilir.');
    else this.file.emit(file);
  }

  protected summary(r: ImportResult): string {
    const duplicates = r.duplicateRows > 0 ? `, ${r.duplicateRows} satır zaten kayıtlıydı` : '';
    return `${r.totalRows} satır işlendi: ${r.importedRows} yeni işlem eklendi${duplicates}.`;
  }
}
