import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { TeacherLoginPage } from '../../pages/teacher-login.page';
import { CreateTemplatePage } from '../../pages/create-template.page';
import { MINIMAL_PDF_BYTES } from '../../fixtures/test-files';
import { loginTeacher, TEACHER_EMAIL, TEACHER_PASSWORD } from '../../fixtures/seed';

test.describe('ERR-003 to ERR-005: Template authoring blocking errors', () => {
  test('ERR-003: Teacher submits setup without template name → inline error', async ({ page }) => {
    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto('/teacher/library/new/setup');
    await page.waitForSelector('#create-setup-form');

    // Select skill without filling name, then attempt to continue
    await page.locator('input[type="radio"][value="reading"]').check();
    await page.locator('#create-setup-continue-button').click();

    const createTemplatePage = new CreateTemplatePage(page);
    await expect(createTemplatePage.getSetupNameError()).toBeVisible();
    expect(page.url()).not.toContain('/materials');
  });

  test('ERR-004: Teacher uploads non-PDF file for PDF slot → rejected with error', async ({
    page,
    apiContext,
  }) => {
    // Create a draft template via API so we can navigate straight to the materials step
    const xsrfToken = await loginTeacher(apiContext);
    const createRes = await apiContext.post('/api/test-templates', {
      data: { title: `E2E ERR004 ${Date.now()}`, skill: 'reading' },
      headers: { 'X-XSRF-TOKEN': xsrfToken },
    });
    expect(createRes.ok()).toBeTruthy();
    const { id: templateId } = await createRes.json() as { id: string };

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto(`/teacher/library/${templateId}/materials`);
    await page.waitForSelector('#create-materials-file-picker');

    const createTemplatePage = new CreateTemplatePage(page);
    await createTemplatePage.uploadInvalidFileForPdf();

    await expect(createTemplatePage.getUploadError()).toBeVisible();
    // Materials file picker remains — template is not corrupted
    await expect(page.locator('#create-materials-file-picker')).toBeVisible();
  });

  test('ERR-005: Teacher continues from answer key with incomplete rows → blocked', async ({
    page,
    apiContext,
  }) => {
    // Create draft template with uploaded PDF so the answer-key step is accessible
    const xsrfToken = await loginTeacher(apiContext);
    const createRes = await apiContext.post('/api/test-templates', {
      data: { title: `E2E ERR005 ${Date.now()}`, skill: 'reading' },
      headers: { 'X-XSRF-TOKEN': xsrfToken },
    });
    expect(createRes.ok()).toBeTruthy();
    const { id: templateId } = await createRes.json() as { id: string };

    const uploadRes = await apiContext.post(`/api/test-templates/${templateId}/materials`, {
      multipart: {
        file: { name: 'test.pdf', mimeType: 'application/pdf', buffer: MINIMAL_PDF_BYTES },
        role: 'pdf',
      },
      headers: { 'X-XSRF-TOKEN': xsrfToken },
    });
    expect(uploadRes.ok()).toBeTruthy();

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto(`/teacher/library/${templateId}/answer-key`);

    // Fill only 2 of 3 answers — leave question 3 blank
    const createTemplatePage = new CreateTemplatePage(page);
    await createTemplatePage.fillAnswerKey(3, ['A', 'B']);

    // Try to continue without completing all rows
    await page.locator('#answer-key-continue-button').click();

    await expect(createTemplatePage.getAnswerKeyMissingCount()).toBeVisible();
    expect(page.url()).not.toContain('/review');
  });
});
