import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { StudentClassEntryPage } from '../../pages/student-class-entry.page';
import { SpeakingSubmissionPage } from '../../pages/speaking-submission.page';
import {
  loginTeacher,
  getClassIdByCode,
  createReadySpeakingTemplate,
  createHomeworkAssignment,
  CLASS_CODE,
  STUDENT_IDENTIFIER,
  STUDENT_PASSWORD,
} from '../../fixtures/seed';

test.describe('HP-003: Student uploads Speaking file and submits', () => {
  test('AC4: Student uploads Speaking audio file and final-submits', async ({
    page,
    apiContext,
  }) => {
    // Fixture: seed speaking template + homework
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadySpeakingTemplate(apiContext, xsrfToken);
    await createHomeworkAssignment(apiContext, xsrfToken, templateId, classId);

    // Student class entry flow
    const classEntryPage = new StudentClassEntryPage(page);
    await classEntryPage.goto();
    await classEntryPage.enterClassCode(CLASS_CODE);
    await expect(classEntryPage.getClassCard()).toBeVisible();
    await classEntryPage.confirmClass();

    // Student login
    await page.waitForURL('**/student/login**');
    await page.locator('#student-login-identifier-input').fill(STUDENT_IDENTIFIER);
    await page.locator('#student-login-password-input').fill(STUDENT_PASSWORD);
    await page.locator('#student-login-submit-button').click();
    await page.waitForURL('**/student/tests');

    // Find and open Speaking homework
    await page.waitForSelector('.item-list');
    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Speaking' }).first();
    await expect(itemCard).toBeVisible();
    await itemCard.getByRole('button', { name: 'Bắt đầu' }).click();

    // Speaking workspace
    await page.waitForURL('**/student/speaking/**');
    const speakingPage = new SpeakingSubmissionPage(page);
    await speakingPage.waitForLoad();

    await expect(speakingPage.getModeBadge()).toContainText('Homework');

    // Upload audio file
    await speakingPage.uploadAudioFile('speaking-test.webm');
    await expect(speakingPage.getDraftFilename()).toContainText('speaking-test.webm');

    // Final submit
    await speakingPage.clickFinalSubmit();

    // Verify modal shows filename and mode
    await expect(speakingPage.getModalFilename()).toContainText('speaking-test.webm');
    await speakingPage.confirmFinalSubmit();

    // Verify success state
    await expect(speakingPage.getSuccessPanel()).toBeVisible();
    await expect(speakingPage.getSuccessFilename()).toContainText('speaking-test.webm');
    await expect(speakingPage.getSuccessSubmittedAt()).not.toBeEmpty();

    // Verify replace/remove not available (final-submit-section gone, success panel shown)
    await expect(page.locator('[data-testid="final-submit-section"]')).not.toBeVisible();
  });
});
