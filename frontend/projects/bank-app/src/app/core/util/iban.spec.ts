import { formatIban } from './iban';

describe('formatIban', () => {
  it('groups by four and leaves the short remainder at the end', () => {
    expect(formatIban('TR330006100519786457841326')).toBe('TR33 0006 1005 1978 6457 8413 26');
  });

  it('does not add a trailing space to an exact multiple of four', () => {
    expect(formatIban('TR330006')).toBe('TR33 0006');
  });

  it('accepts an IBAN that is already grouped', () => {
    expect(formatIban('TR33 0006')).toBe('TR33 0006');
  });
});
