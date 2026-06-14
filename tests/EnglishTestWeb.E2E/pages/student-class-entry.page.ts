import { Page } from '@playwright/test';

export class StudentClassEntryPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/class');
    await this.page.waitForSelector('#student-class-entry-form');
  }

  async enterClassCode(code: string): Promise<void> {
    await this.page.locator('#student-class-entry-code-input').fill(code);
    await this.page.locator('#student-class-entry-submit-button').click();
    await this.page.waitForSelector('#student-class-entry-confirmation');
  }

  async confirmClass(): Promise<void> {
    await this.page.locator('#student-class-entry-confirm-button').click();
    await this.page.waitForURL('**/student/login**');
  }

  getClassCard() {
    return this.page.locator('#student-class-entry-class-card');
  }

  getErrorMessage() {
    return this.page.locator('#student-class-entry-error-alert');
  }
}
