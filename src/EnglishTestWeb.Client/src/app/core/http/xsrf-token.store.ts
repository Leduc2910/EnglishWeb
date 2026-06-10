import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class XsrfTokenStore {
  private tokenValue: string | null = null;

  setToken(token: string): void {
    this.tokenValue = token;
  }

  token(): string | null {
    return this.tokenValue;
  }

  clear(): void {
    this.tokenValue = null;
  }
}
