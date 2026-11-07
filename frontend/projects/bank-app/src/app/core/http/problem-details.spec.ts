import { HttpErrorResponse } from '@angular/common/http';

import { problemErrors, problemMessage } from './problem-details';

const response = (status: number, error: unknown) => new HttpErrorResponse({ status, error });

describe('problemMessage', () => {
  it('uses the detail written for the user', () => {
    const error = response(409, { title: 'Çakışma', detail: 'Bu karta bağlı taksit var.' });

    expect(problemMessage(error)).toBe('Bu karta bağlı taksit var.');
  });

  it('falls back to the first field error of a validation response', () => {
    const error = response(400, {
      title: 'One or more validation errors occurred.',
      errors: { tcNo: ['Geçerli bir TC kimlik numarası giriniz.'], email: ['Geçerli bir e-posta adresi giriniz.'] },
    });

    expect(problemMessage(error)).toBe('Geçerli bir TC kimlik numarası giriniz.');
  });

  it('explains a network failure instead of showing a status code', () => {
    expect(problemMessage(response(0, null))).toContain('Sunucuya ulaşılamadı');
  });

  it('never shows the generic title of a server error', () => {
    const error = response(500, { title: 'Beklenmeyen bir hata oluştu', status: 500 });

    expect(problemMessage(error, 'Kaydedilemedi.')).toBe('Kaydedilemedi.');
  });

  it('handles errors that did not come from HTTP', () => {
    expect(problemMessage(new Error('boom'), 'Olmadı.')).toBe('Olmadı.');
  });
});

describe('problemErrors', () => {
  it('flattens field errors', () => {
    const error = response(400, { errors: { name: ['Ad zorunludur.'], limitAmount: ['Limit sıfırdan büyük olmalıdır.'] } });

    expect(problemErrors(error)).toEqual(['Ad zorunludur.', 'Limit sıfırdan büyük olmalıdır.']);
  });

  it('returns the line list of a rejected import as is', () => {
    const error = response(400, { detail: 'Dosyada hatalı satırlar var.', errors: ['Satır 3: Tarih okunamadı.', 'Satır 4: Açıklama boş.'] });

    expect(problemErrors(error)).toEqual(['Satır 3: Tarih okunamadı.', 'Satır 4: Açıklama boş.']);
  });
});
