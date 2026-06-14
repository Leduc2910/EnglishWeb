import { Page } from '@playwright/test';
import { MINIMAL_PDF_BYTES } from '../fixtures/test-files';

export class CreateTemplatePage {
  constructor(private readonly page: Page) {}

  // Step 1: Setup
  async fillSetup(title: string, skill: 'reading' | 'listening' | 'speaking'): Promise<void> {
    await this.page.waitForSelector('#create-setup-form');
    await this.page.locator('#create-setup-name-input').fill(title);
    await this.page.locator(`input[type="radio"][value="${skill}"]`).check();
  }

  async clickContinueFromSetup(): Promise<void> {
    await this.page.locator('#create-setup-continue-button').click();
    await this.page.waitForURL('**/materials');
  }

  // Step 2: Materials
  async uploadPdf(): Promise<void> {
    await this.page.waitForSelector('#create-materials-file-picker');
    const [fileChooser] = await Promise.all([
      this.page.waitForEvent('filechooser'),
      this.page.locator('#create-materials-file-picker').click(),
    ]);
    await fileChooser.setFiles({
      name: 'test.pdf',
      mimeType: 'application/pdf',
      buffer: MINIMAL_PDF_BYTES,
    });
    await this.page.waitForSelector('#create-materials-file-card.success');
  }

  async clickContinueFromMaterials(): Promise<void> {
    await this.page.locator('#create-materials-continue-button').click();
    await this.page.waitForURL('**/answer-key');
  }

  // Step 3: Answer key
  async fillAnswerKey(questionCount: number, answers: string[]): Promise<void> {
    await this.page.waitForSelector('#answer-key-controls');
    await this.page.locator('#answer-key-question-count-input').fill(String(questionCount));
    await this.page.locator('#answer-key-question-count-input').press('Tab');
    // Wait for grid to render
    await this.page.waitForSelector('#answer-key-grid');
    for (let i = 0; i < answers.length; i++) {
      await this.page
        .getByRole('textbox', { name: `Đáp án câu ${i + 1}` })
        .fill(answers[i]);
    }
  }

  async clickContinueFromAnswerKey(): Promise<void> {
    await this.page.locator('#answer-key-continue-button').click();
    await this.page.waitForURL('**/review');
  }

  // Step 4: Review
  async clickMarkReady(): Promise<void> {
    await this.page.locator('#review-publish-button').click();
    await this.page.waitForSelector('#review-publish-success-banner');
  }

  async isSuccessBannerVisible(): Promise<boolean> {
    return this.page.locator('#review-publish-success-banner').isVisible();
  }

  async clickGoToHomework(): Promise<void> {
    await this.page.locator('#review-create-homework-button').click();
    await this.page.waitForURL('**/homework/new**');
  }

  async clickGoToLiveExam(): Promise<void> {
    await this.page.locator('#review-create-live-exam-button').click();
    await this.page.waitForURL('**/live-exams/new**');
  }

  getHomeworkButton() {
    return this.page.locator('#review-create-homework-button');
  }

  getLiveExamButton() {
    return this.page.locator('#review-create-live-exam-button');
  }

  // Error state helpers
  getSetupNameError() {
    return this.page.locator('#create-setup-form .field-error').first();
  }

  async uploadInvalidFileForPdf(): Promise<void> {
    await this.page.waitForSelector('#create-materials-file-picker');
    const [fileChooser] = await Promise.all([
      this.page.waitForEvent('filechooser'),
      this.page.locator('#create-materials-file-picker').click(),
    ]);
    await fileChooser.setFiles({
      name: 'invalid.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('not a pdf'),
    });
  }

  getUploadError() {
    return this.page.locator('.upload-slot .field-error').first();
  }

  getAnswerKeyMissingCount() {
    return this.page.locator('#answer-key-missing-count');
  }

  getAnswerKeyErrorList() {
    return this.page.locator('.error-list[role="alert"]');
  }

  getMarkReadyButton() {
    return this.page.locator('#review-publish-button');
  }
}
