import { Page } from '@playwright/test';

export class AttemptWorkspacePage {
  constructor(private readonly page: Page) {}

  async waitForLoad(): Promise<void> {
    await this.page.waitForSelector('[data-testid="workspace-header"]');
    await this.page.waitForSelector('[data-testid="pdf-viewer"]');
  }

  async fillAnswer(questionNumber: number, answer: string): Promise<void> {
    await this.page.locator(`[data-testid="answer-input-${questionNumber}"]`).fill(answer);
  }

  async waitForAutosave(): Promise<void> {
    await this.page.waitForSelector('.autosave-saved', { timeout: 10_000 });
  }

  async clickSubmit(): Promise<void> {
    await this.page.locator('[data-testid="submit-button"]').click();
    await this.page.waitForSelector('[data-testid="submit-confirm-modal"]');
  }

  async confirmSubmit(): Promise<void> {
    await this.page.locator('[data-testid="confirm-submit-btn"]').click();
    await this.page.waitForSelector('[data-testid="submit-success"]');
  }

  getModeBadge() {
    return this.page.locator('.mode-badge');
  }

  getAutosaveStatus() {
    return this.page.locator('[data-testid="autosave-status"]');
  }

  getSubmitSuccess() {
    return this.page.locator('[data-testid="submit-success"]');
  }

  getResultTemplateTitle() {
    return this.page.locator('[data-testid="result-template-title"]');
  }

  getResultSubmittedAt() {
    return this.page.locator('[data-testid="result-submitted-at"]');
  }

  getMissingAnswerWarningModal() {
    return this.page.locator('[data-testid="submit-confirm-modal"]');
  }

  getMissingAnswerCount() {
    return this.page.locator('[data-testid="confirm-missing-count"]');
  }

  getBackFromModalButton() {
    return this.page.locator('[data-testid="cancel-submit-btn"]');
  }
}
