import { normalizeClassCode } from './class-code';

describe('normalizeClassCode', () => {
  it('strips spaces and uppercases valid codes', () => {
    expect(normalizeClassCode(' eng 7a ')).toBe('ENG7A');
  });
});
