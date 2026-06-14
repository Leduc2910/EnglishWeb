import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { TeacherLoginPage } from '../../pages/teacher-login.page';
import { StudentClassEntryPage } from '../../pages/student-class-entry.page';
import { SpeakingSubmissionPage } from '../../pages/speaking-submission.page';
import { ResultsGradingPage } from '../../pages/results-grading.page';
import {
  loginTeacher,
  getClassIdByCode,
  createReadySpeakingTemplate,
  createReadyReadingTemplate,
  createHomeworkAssignment,
  createExpiredHomeworkAssignment,
  createLiveExamSession,
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

test.describe('ERR-006 to ERR-009: Submission and session blocking errors', () => {
  test('ERR-006: Student tries to final-submit speaking without uploading audio → blocked', async ({
    page,
    apiContext,
  }) => {
    // Create a speaking homework so the student can start an attempt
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadySpeakingTemplate(apiContext, xsrfToken);
    await createHomeworkAssignment(apiContext, xsrfToken, templateId, classId);

    await loginStudentViaClassEntry(page);

    await page.waitForSelector('.item-list');
    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Speaking' }).first();
    await expect(itemCard).toBeVisible();
    await itemCard.getByRole('button', { name: 'Bắt đầu' }).click();

    // Speaking workspace — do NOT upload any audio
    const speakingPage = new SpeakingSubmissionPage(page);
    await speakingPage.waitForLoad();

    // Final submit button should be present but will be blocked
    const finalSubmitBtn = speakingPage.getFinalSubmitButton();
    await expect(finalSubmitBtn).toBeVisible();

    // No-file hint should be visible since no audio has been uploaded
    await expect(speakingPage.getNoFileHint()).toBeVisible();

    // Clicking final submit without a file should keep the hint or show error
    await finalSubmitBtn.click();
    await expect(speakingPage.getNoFileHint()).toBeVisible();
  });

  test('ERR-007: Teacher enters out-of-range score for speaking → grade error shown', async ({
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
    await expect(speakingRow).toBeVisible();
    await speakingRow.click();
    await resultsPage.waitForDetailPanel();

    // Fill score outside valid range 0–10
    await resultsPage.getScoreInput().fill('111');
    await resultsPage.getSaveButton().click();

    await expect(resultsPage.getGradeError()).toBeVisible();
    await expect(resultsPage.getGradeSuccess()).not.toBeVisible();
  });

  test('ERR-008: Student cannot start expired homework — item is blocked', async ({
    page,
    apiContext,
  }) => {
    // Expired homework: deadline is 5 minutes in the past
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadyReadingTemplate(apiContext, xsrfToken);
    await createExpiredHomeworkAssignment(apiContext, xsrfToken, templateId, classId);

    await loginStudentViaClassEntry(page);

    await page.waitForSelector('.item-list');
    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Reading' }).first();
    await expect(itemCard).toBeVisible();

    // Expired homework must show an expired status indicator
    await expect(itemCard).toContainText(/hết hạn/i);
    // And the start button must not be usable
    const startBtn = itemCard.getByRole('button', { name: 'Bắt đầu' });
    await expect(startBtn).not.toBeEnabled();
  });

  test('ERR-009: Student cannot start live exam session that has not been opened', async ({
    page,
    apiContext,
  }) => {
    // Live exam session exists but teacher has NOT called /open
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadyReadingTemplate(apiContext, xsrfToken);
    await createLiveExamSession(apiContext, xsrfToken, templateId, classId);

    await loginStudentViaClassEntry(page);

    await page.waitForSelector('.item-list');

    // Live-exam tab should appear once the seeded session is visible to the student
    const liveExamTab = page.locator('#assigned-tests-tab-live-exam');
    await expect(liveExamTab).toBeVisible();
    await liveExamTab.click();
    await page.waitForSelector('.item-list');

    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Reading' }).first();
    await expect(itemCard).toBeVisible();

    // Unopened live exam must show a "not yet open" indicator
    await expect(itemCard).toContainText(/chưa mở/i);
    // And the start button must not be usable
    const startBtn = itemCard.getByRole('button', { name: 'Bắt đầu' });
    await expect(startBtn).not.toBeEnabled();
  });
});
