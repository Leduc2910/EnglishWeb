import { Page } from '@playwright/test';

export class StudentTestsPage {
  constructor(private readonly page: Page) {}

  async waitForLoad(): Promise<void> {
    await this.page.waitForSelector('.student-tests-page');
    await this.page.waitForSelector('.tab-bar');
  }

  async openHomeworkItem(titleFragment: string): Promise<void> {
    const card = this.page.locator('.item-card').filter({ hasText: titleFragment });
    await card.getByRole('button', { name: 'Bắt đầu' }).click();
  }

  async openHomeworkTab(): Promise<void> {
    await this.page.locator('#assigned-tests-tab-homework').click();
  }

  getItemCard(titleFragment: string) {
    return this.page.locator('.item-card').filter({ hasText: titleFragment });
  }
}
