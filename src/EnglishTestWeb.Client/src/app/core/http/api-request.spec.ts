import { isApiRequest } from './api-request';

describe('isApiRequest', () => {
  it('matches relative /api paths case-insensitively', () => {
    expect(isApiRequest('/api/health')).toBe(true);
    expect(isApiRequest('/API/health')).toBe(true);
  });

  it('matches absolute API URLs by pathname', () => {
    expect(isApiRequest('https://localhost:5124/api/health')).toBe(true);
  });

  it('rejects non-api paths', () => {
    expect(isApiRequest('/assets/logo.svg')).toBe(false);
  });
});
