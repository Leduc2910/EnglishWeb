import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { TeacherLoginPage } from '../../pages/teacher-login.page';
import { StudentClassEntryPage } from '../../pages/student-class-entry.page';
import { CreateTemplatePage } from '../../pages/create-template.page';
import { SpeakingSubmissionPage } from '../../pages/speaking-submission.page';
import { ResultsGradingPage } from '../../pages/results-grading.page';
import { MINIMAL_PDF_BYTES } from '../../fixtures/test-files';
import {
  loginTeacher,
  getClassIdByCode,
  createReadySpeakingTemplate,
  createHomeworkAssignment,
  seedSubmittedSpeakingChain,
  CLASS_CODE,
  TEACHER_EMAIL,
  TEACHER_PASSWORD,
  STUDENT_IDENTIFIER,
  STUDENT_PASSWORD,
} from '../../fixtures/seed';

async function loginStudentViaClassEntry(
  page: import('@playwright/test').Page,
): Promise<void> {
  const classEntryPage = new StudentClassEntryPage(page);
  await classEntryPage.goto();
  await classEntryPage.enterClassCode(CLASS_CODE);
  await expect(classEntryPage.getClassCard()).toBeVisible();
  await classEntryPage.confirmClass();
  await page.waitForURL('**/student/login**');
  await page.locator('#student-login-identifier-input').fill(STUDENT_IDENTIFIER);
  await page.locator('#student-login-password-input').fill(STUDENT_PASSWORD);
  await page.locator('#student-login-submit-button').click();
  await page.waitForURL('**/student/tests');
}

test.describe('EDGE-004: Single-fire actions protect against duplicate submissions', () => {
  test('EDGE-004a: Mark-ready button is disabled after template is published', async ({
    page,
    apiContext,
  }) => {
    // Create draft template with PDF + answer key, ready to publish
    const xsrfToken = await loginTeacher(apiContext);
    const createRes = await apiContext.post('/api/test-templates', {
      data: { title: `E2E EDGE004a ${Date.now()}`, skill: 'reading' },
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

    const answerKeyRes = await apiContext.put(`/api/test-templates/${templateId}/answer-key`, {
      data: {
        questionCount: 3,
        scoringMode: 'equal',
        totalScore: 10,
        rows: [
          { questionNumber: 1, correctAnswer: 'A', score: null },
          { questionNumber: 2, correctAnswer: 'B', score: null },
          { questionNumber: 3, correctAnswer: 'C', score: null },
        ],
      },
      headers: { 'X-XSRF-TOKEN': xsrfToken },
    });
    expect(answerKeyRes.ok()).toBeTruthy();

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto(`/teacher/library/${templateId}/review`);

    const createTemplatePage = new CreateTemplatePage(page);
    await createTemplatePage.clickMarkReady();

    // Success banner visible
    await expect(page.locator('#review-publish-success-banner')).toBeVisible();

    // Publish button should be disabled after successful publish
    await expect(createTemplatePage.getMarkReadyButton()).toBeDisabled();
  });

  test('EDGE-004b: Speaking final-submit button absent after successful submission', async ({
    page,
    apiContext,
  }) => {
    // Create speaking homework
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadySpeakingTemplate(apiContext, xsrfToken);
    await createHomeworkAssignment(apiContext, xsrfToken, templateId, classId);

    await loginStudentViaClassEntry(page);

    await page.waitForSelector('.item-list');
    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Speaking' }).first();
    await expect(itemCard).toBeVisible();
    await itemCard.getByRole('button', { name: 'Bắt đầu' }).click();

    const speakingPage = new SpeakingSubmissionPage(page);
    await speakingPage.waitForLoad();

    // Upload audio and submit
    await speakingPage.uploadAudioFile();
    await speakingPage.clickFinalSubmit();
    await speakingPage.confirmFinalSubmit();

    // Success panel visible
    await expect(speakingPage.getSuccessPanel()).toBeVisible();

    // Final-submit button should be gone after success
    await expect(speakingPage.getFinalSubmitButton()).not.toBeVisible();
  });

  test('EDGE-004c: Grading save button is disabled after score is saved', async ({
    page,
    apiContext,
  }) => {
    await seedSubmittedSpeakingChain(apiContext);

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    const resultsPage = new ResultsGradingPage(page);
    await resultsPage.goto();
    await resultsPage.filterByStatus('submitted');

    await expect(resultsPage.getResultRows()).not.toHaveCount(0);

    const speakingRow = resultsPage.getResultRows().filter({ hasText: 'Speaking' }).first();
    await speakingRow.click();
    await resultsPage.waitForDetailPanel();

    await resultsPage.getScoreInput().fill('7');
    await page.locator('#feedbackInput').fill('Good work');
    await resultsPage.getSaveButton().click();

    await expect(resultsPage.getGradeSuccess()).toBeVisible();

    // Save button should be disabled after successful save to prevent double-submit
    await expect(resultsPage.getSaveButton()).toBeDisabled();
  });
});
