import { isProblemDetails, readProblemCode } from './problem-details';

describe('isProblemDetails', () => {
  it('accepts RFC7807-shaped payloads', () => {
    expect(isProblemDetails({ title: 'Bad Request', status: 400 })).toBe(true);
    expect(isProblemDetails({ extensions: { code: 'auth.xsrfRequired' } })).toBe(true);
  });

  it('rejects non-problem payloads', () => {
    expect(isProblemDetails(null)).toBe(false);
    expect(isProblemDetails([])).toBe(false);
    expect(isProblemDetails({ message: 'plain error' })).toBe(false);
  });
});

describe('readProblemCode', () => {
  it('reads a top-level code when present', () => {
    expect(readProblemCode({ code: 'auth.xsrfRequired' })).toBe('auth.xsrfRequired');
  });

  it('reads an extensions code when top-level code is absent', () => {
    expect(readProblemCode({ extensions: { code: 'auth.xsrfInvalid' } })).toBe(
      'auth.xsrfInvalid',
    );
  });

  it('returns undefined for empty code values', () => {
    expect(readProblemCode({ code: '' })).toBeUndefined();
  });
});
