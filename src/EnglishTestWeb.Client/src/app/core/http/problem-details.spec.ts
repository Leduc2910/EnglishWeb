import { readProblemCode } from './problem-details';

describe('readProblemCode', () => {
  it('reads a top-level code when present', () => {
    expect(readProblemCode({ code: 'auth.xsrfRequired' })).toBe('auth.xsrfRequired');
  });

  it('reads an extensions code when top-level code is absent', () => {
    expect(readProblemCode({ extensions: { code: 'auth.xsrfInvalid' } })).toBe(
      'auth.xsrfInvalid',
    );
  });
});
