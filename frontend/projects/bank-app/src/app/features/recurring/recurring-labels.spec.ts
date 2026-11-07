import { describeSchedule } from './recurring-labels';

describe('describeSchedule', () => {
  it('reads naturally in Turkish', () => {
    expect(describeSchedule('Monthly', 1)).toBe('Her ay');
    expect(describeSchedule('Monthly', 3)).toBe('3 ayda bir');
    expect(describeSchedule('Weekly', 2)).toBe('2 haftada bir');
    expect(describeSchedule('Yearly', 1)).toBe('Her yıl');
  });
});
