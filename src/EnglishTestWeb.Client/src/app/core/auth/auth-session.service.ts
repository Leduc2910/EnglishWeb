import { Injectable } from '@angular/core';

/**
 * Baseline auth session uses same-origin Identity cookies only.
 * Browser token storage is intentionally not used in this MVP stack.
 */
@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  readonly usesBrowserTokenStorage = false;

  persistAccessToken(_token: string): void {
    throw new Error('Browser token storage is disabled for EnglishTestWeb.');
  }
}
