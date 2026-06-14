import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { StudentClassEntryPage } from '../../pages/student-class-entry.page';
import { AttemptWorkspacePage } from '../../pages/attempt-workspace.page';
import {
  loginTeacher,
  getClassIdByCode,
  createReadyReadingTemplate,
  createHomeworkAssignment,
  CLASS_CODE,
  STUDENT_IDENTIFIER,
  STUDENT_PASSWORD,
} from '../../fixtures/seed';

test.describe('HP-002: Student completes Reading homework', () => {
  test('AC3: Student enters class, logs in, completes Reading homework, and submits', async ({
    page,
    apiContext,
  }) => {
    // Fixture: seed ready reading template + homework assignment
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadyReadingTemplate(apiContext, xsrfToken);
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

    // Find and open the Reading homework item
    await page.waitForSelector('.student-tests-page');
    await page.waitForSelector('.item-list');

    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Reading' }).first();
    await expect(itemCard).toBeVisible();
    await itemCard.getByRole('button', { name: 'Bắt đầu' }).click();

    // Workspace
    const workspace = new AttemptWorkspacePage(page);
    await workspace.waitForLoad();

    await expect(workspace.getModeBadge()).toContainText('Homework');

    // Enter 3 answers
    await workspace.fillAnswer(1, 'A');
    await workspace.fillAnswer(2, 'B');
    await workspace.fillAnswer(3, 'C');

    // Wait for autosave
    await workspace.waitForAutosave();

    // Submit
    await workspace.clickSubmit();
    await workspace.confirmSubmit();

    // Verify success
    await expect(workspace.getSubmitSuccess()).toBeVisible();
    await expect(workspace.getResultTemplateTitle()).toContainText('E2E Reading');
    await expect(workspace.getResultSubmittedAt()).not.toBeEmpty();

    // Verify answers are locked (inputs disabled)
    const firstAnswerInput = page.locator('[data-testid="answer-input-1"]');
    await expect(firstAnswerInput).toBeDisabled();
  });
});
