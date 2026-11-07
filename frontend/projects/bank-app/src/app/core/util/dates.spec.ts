import { toIsoDate } from './dates';

describe('toIsoDate', () => {
  it('uses the local calendar date, not the UTC one', () => {
    // 00:30 local time; in any zone east of UTC, toISOString() would still say the previous day.
    const justAfterMidnight = new Date(2026, 3, 1, 0, 30);

    expect(toIsoDate(justAfterMidnight)).toBe('2026-04-01');
  });

  it('pads month and day', () => {
    expect(toIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });
});
