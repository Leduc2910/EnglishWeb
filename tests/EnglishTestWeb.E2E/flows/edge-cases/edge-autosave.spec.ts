import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { StudentClassEntryPage } from '../../pages/student-class-entry.page';
import { AttemptWorkspacePage } from '../../pages/attempt-workspace.page';
import {
  loginTeacher,
  getClassIdByCode,
  createReadyReadingTemplate,
  createHomeworkAssignment,
  seedNotSubmittedReadingChain,
  CLASS_CODE,
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

test.describe('EDGE-002 and EDGE-003: Autosave and missing-answer edge cases', () => {
  test('EDGE-002: Student answers saved server-side are restored when revisiting the attempt', async ({
    page,
    apiContext,
  }) => {
    // Seed: create attempt with 2 of 3 answers saved server-side
    await seedNotSubmittedReadingChain(apiContext);

    await loginStudentViaClassEntry(page);

    await page.waitForSelector('.item-list');
    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Reading' }).first();
    await expect(itemCard).toBeVisible();

    // Continue (attempt exists) or Start — match both Vietnamese labels
    await itemCard.getByRole('button', { name: /bắt đầu|tiếp tục/i }).click();

    const workspace = new AttemptWorkspacePage(page);
    await workspace.waitForLoad();

    // Answers 1 and 2 should be pre-filled from the server-saved draft on first load
    await expect(page.locator('[data-testid="answer-input-1"]')).toHaveValue('A');
    await expect(page.locator('[data-testid="answer-input-2"]')).toHaveValue('B');
    // Answer 3 was never saved — should be blank
    await expect(page.locator('[data-testid="answer-input-3"]')).toHaveValue('');

    // AC2: reload the page and verify answers are STILL restored from server
    await page.reload();
    await workspace.waitForLoad();
    await expect(page.locator('[data-testid="answer-input-1"]')).toHaveValue('A');
    await expect(page.locator('[data-testid="answer-input-2"]')).toHaveValue('B');
    await expect(page.locator('[data-testid="answer-input-3"]')).toHaveValue('');
  });

  test('EDGE-003: Submit with missing answers shows warning modal with correct count; Back returns to workspace', async ({
    page,
    apiContext,
  }) => {
    // Create a fresh reading homework — student will leave 2 of 3 answers blank
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadyReadingTemplate(apiContext, xsrfToken);
    await createHomeworkAssignment(apiContext, xsrfToken, templateId, classId);

    await loginStudentViaClassEntry(page);

    await page.waitForSelector('.item-list');
    const itemCard = page.locator('.item-card').filter({ hasText: 'E2E Reading' }).first();
    await expect(itemCard).toBeVisible();
    await itemCard.getByRole('button', { name: 'Bắt đầu' }).click();

    const workspace = new AttemptWorkspacePage(page);
    await workspace.waitForLoad();

    // Fill only question 1; leave questions 2 and 3 empty
    await workspace.fillAnswer(1, 'A');

    // Trigger submit
    await workspace.clickSubmit();

    // Warning modal should appear
    await expect(workspace.getMissingAnswerWarningModal()).toBeVisible();

    // Missing answer count should reflect 2 unanswered questions
    const missingCount = workspace.getMissingAnswerCount();
    await expect(missingCount).toBeVisible();
    await expect(missingCount).toContainText('2');

    // Clicking Back closes modal and returns to workspace (does not submit)
    await workspace.getBackFromModalButton().click();
    await expect(workspace.getMissingAnswerWarningModal()).not.toBeVisible();
    await expect(page.locator('[data-testid="workspace-header"]')).toBeVisible();
    // Answer filled before submitting must be preserved (not cleared by modal dismiss)
    await expect(page.locator('[data-testid="answer-input-1"]')).toHaveValue('A');
  });
});
