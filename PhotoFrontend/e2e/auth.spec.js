import { test, expect } from '@playwright/test';

test.describe('Authentication & Session E2E Flows', () => {
  test('should navigate to login page and display form elements', async ({ page }) => {
    await page.goto('/login');

    await expect(page).toHaveTitle(/Pixel Lyra/i);
    await expect(page.locator('#username')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('#password')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('should handle ejected status query parameter gracefully', async ({ page }) => {
    await page.goto('/login?ejected=true');

    await expect(page).toHaveURL(/login/);
    await expect(page.locator('#username')).toBeVisible();
  });
});
