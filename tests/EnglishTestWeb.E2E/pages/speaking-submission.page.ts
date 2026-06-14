import { Page } from '@playwright/test';
import { MINIMAL_WEBM_BYTES } from '../fixtures/test-files';

export class SpeakingSubmissionPage {
  constructor(private readonly page: Page) {}

  async waitForLoad(): Promise<void> {
    await this.page.waitForSelector('[data-testid="prompt-card"]');
    await this.page.waitForSelector('[data-testid="upload-card"]');
  }

  async uploadAudioFile(fileName = 'speaking-test.webm'): Promise<void> {
    const [fileChooser] = await Promise.all([
      this.page.waitForEvent('filechooser'),
      this.page.locator('[data-testid="file-input"]').click(),
    ]);
    await fileChooser.setFiles({
      name: fileName,
      mimeType: 'audio/webm',
      buffer: MINIMAL_WEBM_BYTES,
    });
    await this.page.locator('[data-testid="upload-button"]').click();
    await this.page.waitForSelector('[data-testid="draft-file"]');
  }

  async clickFinalSubmit(): Promise<void> {
    await this.page.locator('[data-testid="final-submit-btn"]').click();
    await this.page.waitForSelector('[data-testid="confirm-modal"]');
  }

  async confirmFinalSubmit(): Promise<void> {
    await this.page.locator('[data-testid="confirm-submit-btn"]').click();
    await this.page.waitForSelector('[data-testid="success-panel"]');
  }

  getDraftFilename() {
    return this.page.locator('[data-testid="draft-filename"]');
  }

  getSuccessPanel() {
    return this.page.locator('[data-testid="success-panel"]');
  }

  getSuccessFilename() {
    return this.page.locator('[data-testid="success-filename"]');
  }

  getSuccessSubmittedAt() {
    return this.page.locator('[data-testid="success-submitted-at"]');
  }

  getModalFilename() {
    return this.page.locator('[data-testid="modal-filename"]');
  }

  getModeBadge() {
    return this.page.locator('[data-testid="mode-badge"]');
  }
}
