import { Page } from '@playwright/test';

export class TeacherLibraryPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/teacher/library');
    await this.page.waitForSelector('.template-library');
  }

  async clickCreateNew(): Promise<void> {
    await this.page.getByRole('link', { name: 'Tạo đề mới' }).click();
    await this.page.waitForURL('**/library/new/setup');
  }

  async openActionsMenu(templateTitle: string): Promise<void> {
    const row = this.page.locator('tbody tr').filter({ hasText: templateTitle });
    await row.getByRole('button', { name: 'Hành động' }).click();
  }

  async clickHomeworkAction(templateTitle: string): Promise<void> {
    await this.openActionsMenu(templateTitle);
    const row = this.page.locator('tbody tr').filter({ hasText: templateTitle });
    await row.getByRole('button', { name: 'Giao homework' }).click();
    await this.page.waitForURL('**/homework/new**');
  }

  async clickLiveExamAction(templateTitle: string): Promise<void> {
    await this.openActionsMenu(templateTitle);
    const row = this.page.locator('tbody tr').filter({ hasText: templateTitle });
    await row.getByRole('button', { name: 'Tạo phiên thi trực tiếp' }).click();
    await this.page.waitForURL('**/live-exams/new**');
  }

  async getTemplateStatusBadge(templateTitle: string): Promise<string | null> {
    const row = this.page.locator('tbody tr').filter({ hasText: templateTitle });
    return row.locator('.status-badge').textContent();
  }
}
