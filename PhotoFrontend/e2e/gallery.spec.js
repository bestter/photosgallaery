import { test, expect } from '@playwright/test';

test.describe('Gallery & Public Features E2E Flows', () => {
  test('should redirect unauthenticated users visiting / to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/login/);
  });

  test('should load main gallery page when authenticated', async ({ page }) => {
    await page.addInitScript(() => {
      const userInfo = {
        role: 'Admin',
        username: 'testuser',
        exp: Math.floor(Date.now() / 1000) + 3600,
      };
      localStorage.setItem('user_info:v1', JSON.stringify(userInfo));
    });

    await page.goto('/');
    await expect(page).toHaveTitle(/Pixel Lyra/i);
    await expect(page.locator('header')).toBeVisible();
  });
});
