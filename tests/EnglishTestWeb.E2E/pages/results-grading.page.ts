import { Page } from '@playwright/test';

export class ResultsGradingPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/teacher/results');
    await this.page.waitForSelector('.results-page');
  }

  async filterByStatus(status: string): Promise<void> {
    await this.page.selectOption('[aria-label="Lọc theo trạng thái"]', status);
    await this.page.getByRole('button', { name: 'Tìm' }).click();
    await this.page.waitForSelector('.results-table, .empty-state');
  }

  async clickResultRow(studentNameFragment: string): Promise<void> {
    const row = this.page.locator('.results-table tbody tr').filter({ hasText: studentNameFragment });
    await row.click();
    await this.page.waitForSelector('.detail-panel');
  }

  async waitForDetailPanel(): Promise<void> {
    await this.page.waitForSelector('.detail-panel');
    await this.page.waitForSelector('.speaking-detail, .rl-detail');
  }

  async playSpeakingAudio(): Promise<void> {
    await this.page.waitForSelector('.audio-player audio');
  }

  async fillGradeForm(score: number, feedback: string): Promise<void> {
    await this.page.locator('#scoreInput').fill(String(score));
    await this.page.locator('#feedbackInput').fill(feedback);
  }

  async saveGrading(): Promise<void> {
    await this.page.locator('.save-btn').click();
    await this.page.waitForSelector('.grade-success', { timeout: 10_000 });
  }

  getResultRows() {
    return this.page.locator('.results-table tbody tr');
  }

  getDetailPanel() {
    return this.page.locator('.detail-panel');
  }

  getAudioPlayer() {
    return this.page.locator('.audio-player audio');
  }

  getGradeSuccess() {
    return this.page.locator('.grade-success');
  }
}
