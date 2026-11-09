import { isValidIban } from './iban';

describe('isValidIban', () => {
  it('accepts a correct IBAN, with or without spaces', () => {
    expect(isValidIban('TR330006100519786457841326')).toBe(true);
    expect(isValidIban('TR33 0006 1005 1978 6457 8413 26')).toBe(true);
  });

  it('rejects a single mistyped digit', () => {
    expect(isValidIban('TR330006100519786457841327')).toBe(false);
  });

  it('rejects something that is not an IBAN at all', () => {
    expect(isValidIban('')).toBe(false);
    expect(isValidIban('12345')).toBe(false);
  });
});
