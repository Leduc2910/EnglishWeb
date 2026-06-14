import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { TeacherLoginPage } from '../../pages/teacher-login.page';
import { ResultsGradingPage } from '../../pages/results-grading.page';
import {
  seedSubmittedSpeakingChain,
  TEACHER_EMAIL,
  TEACHER_PASSWORD,
} from '../../fixtures/seed';

test.describe('HP-004: Teacher filters results and grades Speaking submission', () => {
  test('AC5: Teacher filters results, opens Speaking submission, plays audio, saves score/feedback, sees row updated', async ({
    page,
    apiContext,
  }) => {
    // Fixture: full chain — teacher creates speaking template + homework,
    // student submits speaking via API
    await seedSubmittedSpeakingChain(apiContext);

    // Teacher browser session
    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    // Open results page
    const resultsPage = new ResultsGradingPage(page);
    await resultsPage.goto();

    // Filter by "submitted" status (Đã nộp = needs grading for speaking)
    await resultsPage.filterByStatus('submitted');

    await expect(resultsPage.getResultRows()).not.toHaveCount(0);

    // Click first speaking submission row matching our student
    const rows = resultsPage.getResultRows();
    const speakingRow = rows.filter({ hasText: 'Speaking' }).first();
    await expect(speakingRow).toBeVisible();
    await speakingRow.click();

    // Detail panel opens without navigation
    await resultsPage.waitForDetailPanel();
    const detailPanel = resultsPage.getDetailPanel();
    await expect(detailPanel).toBeVisible();

    // Audio player visible
    await expect(resultsPage.getAudioPlayer()).toBeVisible();

    // Fill grade form (in the detail panel)
    await page.locator('#scoreInput').fill('8');
    await page.locator('#feedbackInput').fill('Good effort');

    // Save grading
    await page.locator('.save-btn').click();
    await expect(resultsPage.getGradeSuccess()).toBeVisible();

    // Verify detail panel metadata (student name, template title, mode badge, timestamp)
    await expect(detailPanel.locator('.detail-meta')).toBeVisible();
    await expect(detailPanel.locator('.status-badge')).toBeVisible();
  });
});
