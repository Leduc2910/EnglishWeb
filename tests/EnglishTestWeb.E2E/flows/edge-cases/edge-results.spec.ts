import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { TeacherLoginPage } from '../../pages/teacher-login.page';
import { ResultsGradingPage } from '../../pages/results-grading.page';
import { seedSubmittedSpeakingChain, TEACHER_EMAIL, TEACHER_PASSWORD } from '../../fixtures/seed';

test.describe('EDGE-005: Results page edge cases', () => {
  test('EDGE-005: No-match search shows empty state; clearing filter restores results', async ({
    page,
    apiContext,
  }) => {
    await seedSubmittedSpeakingChain(apiContext);

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    const resultsPage = new ResultsGradingPage(page);
    await resultsPage.goto();

    // First confirm rows are visible before applying any filter
    await expect(resultsPage.getResultRows()).not.toHaveCount(0);

    // Search for a student name that does not exist
    await resultsPage.fillStudentSearch('ZZZNOMATCH99999');

    // Empty state should be displayed
    await expect(resultsPage.getEmptyState()).toBeVisible();
    await expect(resultsPage.getResultRows()).toHaveCount(0);

    // Clear-filters button in the empty state resets the search
    await expect(resultsPage.getClearFiltersButton()).toBeVisible();
    await resultsPage.getClearFiltersButton().click();
    await page.waitForSelector('.results-table, .empty-state');

    // Results should be visible again after clearing filters
    await expect(resultsPage.getResultRows()).not.toHaveCount(0);
  });
});

// EDGE-006: Missing-file grading error is intentionally skipped.
// The E2E environment has no API to delete a file after upload, and direct DB
// access is out of scope for browser tests. The "file-missing" CSS selector
// added to ResultsGradingPage.getMissingFileError() is tested at unit level
// via the Angular component; the server-side guard is covered by API tests.
