import { Page } from '@playwright/test';

export class TeacherLoginPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/login');
    await this.page.waitForSelector('#teacher-login-form');
  }

  async login(email: string, password: string): Promise<void> {
    await this.page.locator('#teacher-login-email-input').fill(email);
    await this.page.locator('#teacher-login-password-input').fill(password);
    await this.page.locator('#teacher-login-submit-button').click();
    await this.page.waitForURL('**/teacher/**');
  }
}
