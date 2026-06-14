import { expect } from '@playwright/test';
import { test } from '../../fixtures/test-fixtures';
import { TeacherLoginPage } from '../../pages/teacher-login.page';
import { TeacherLibraryPage } from '../../pages/teacher-library.page';
import { CreateTemplatePage } from '../../pages/create-template.page';
import {
  loginTeacher,
  getClassIdByCode,
  createReadyReadingTemplate,
  TEACHER_EMAIL,
  TEACHER_PASSWORD,
  CLASS_CODE,
} from '../../fixtures/seed';

const TEMPLATE_TITLE = `E2E HP001 Reading ${Date.now()}`;

test.describe('HP-001: Teacher creates Reading template and assigns work', () => {
  test('AC1: Teacher creates Reading template through UI and marks ready', async ({ page }) => {
    const loginPage = new TeacherLoginPage(page);
    const createPage = new CreateTemplatePage(page);

    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto('/teacher/library/new/setup');
    await page.waitForSelector('#create-setup-form');

    await createPage.fillSetup(TEMPLATE_TITLE, 'reading');
    await createPage.clickContinueFromSetup();

    await createPage.uploadPdf();
    await createPage.clickContinueFromMaterials();

    await createPage.fillAnswerKey(3, ['A', 'B', 'C']);
    await createPage.clickContinueFromAnswerKey();

    await createPage.clickMarkReady();

    await expect(page.locator('#review-publish-success-banner')).toBeVisible();
    await expect(createPage.getHomeworkButton()).toBeVisible();
    await expect(createPage.getLiveExamButton()).toBeVisible();
  });

  test('AC2a: Teacher creates Homework from seeded ready Reading template', async ({
    page,
    apiContext,
  }) => {
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadyReadingTemplate(apiContext, xsrfToken);

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto(`/teacher/homework/new?templateId=${templateId}`);
    await page.waitForSelector('#homework-create-class-select');

    await page.selectOption('#homework-create-class-select', classId);

    const futureDate = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
    const dateStr = futureDate.toISOString().slice(0, 16);
    await page.locator('#homework-create-due-date-input').fill(dateStr);

    await page.locator('#homework-create-submit').click();
    await page.waitForSelector('#homework-create-success');

    await expect(page.locator('#homework-create-success')).toBeVisible();
    await expect(page.locator('#homework-create-success')).toContainText('Đã giao homework');
  });

  test('AC2b: Teacher creates Live Exam from seeded ready Reading template and opens it', async ({
    page,
    apiContext,
  }) => {
    const xsrfToken = await loginTeacher(apiContext);
    const classId = await getClassIdByCode(apiContext, CLASS_CODE);
    const templateId = await createReadyReadingTemplate(apiContext, xsrfToken);

    const loginPage = new TeacherLoginPage(page);
    await loginPage.goto();
    await loginPage.login(TEACHER_EMAIL, TEACHER_PASSWORD);

    await page.goto(`/teacher/live-exams/new?templateId=${templateId}`);
    await page.waitForSelector('[data-testid="class-select"]');

    await page.selectOption('[data-testid="class-select"]', classId);
    await page.locator('[data-testid="create-action"]').click();
    await page.waitForSelector('[data-testid="session-status-badge"]');

    const statusBadge = page.locator('[data-testid="session-status-badge"]');
    await expect(statusBadge).toContainText('Đã lên lịch');

    await page.locator('[data-testid="open-action"]').click();
    await expect(statusBadge).toContainText('Đang mở');
  });
});
