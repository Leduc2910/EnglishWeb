import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { StudentClassEntryPage } from '../../pages/student-class-entry.page';
import { CLASS_CODE, STUDENT_IDENTIFIER, STUDENT_PASSWORD } from '../../fixtures/seed';

test.describe('ERR-001 and ERR-002: Access blocking errors', () => {
  test('ERR-001: Invalid class code shows error, class card not exposed', async ({ page }) => {
    const classEntryPage = new StudentClassEntryPage(page);
    await classEntryPage.goto();

    await page.locator('#student-class-entry-code-input').fill('INVALIDXXX');
    await page.locator('#student-class-entry-submit-button').click();

    await expect(classEntryPage.getErrorMessage()).toBeVisible();
    await expect(classEntryPage.getClassCard()).not.toBeVisible();
    expect(page.url()).toContain('/class');
  });

  test('ERR-002: Wrong student credentials at class login page blocks entry', async ({ page }) => {
    const classEntryPage = new StudentClassEntryPage(page);
    await classEntryPage.goto();
    await classEntryPage.enterClassCode(CLASS_CODE);

    await expect(classEntryPage.getClassCard()).toBeVisible();
    await classEntryPage.confirmClass();

    await page.waitForURL('**/student/login**');
    await page.locator('#student-login-identifier-input').fill('nobody@invalid.test');
    await page.locator('#student-login-password-input').fill('WrongPass999!');
    await page.locator('#student-login-submit-button').click();

    await expect(page.locator('#student-login-error-alert')).toBeVisible();
    expect(page.url()).not.toContain('/student/tests');

    // Also verify correct credentials still work (identity not leaked)
    await page.locator('#student-login-identifier-input').fill(STUDENT_IDENTIFIER);
    await page.locator('#student-login-password-input').fill(STUDENT_PASSWORD);
    await page.locator('#student-login-submit-button').click();
    await page.waitForURL('**/student/tests');
  });
});
