import { expect, test } from '../../apps/web/node_modules/@playwright/test';

for (const port of [4200, 4300]) {
  for (const width of [320, 375, 768, 1024, 1440]) {
    test(`Kipo responde desde ${port} a ${width} px`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(`https://localhost:${port}`);
      await expect(page.getByText('Kipo: conexión establecida con el servicio v1.')).toBeVisible();
      await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
      expect(overflow).toBe(false);
      await page.getByRole('button', { name: 'Comprobar conexión' }).focus();
      await page.keyboard.press('Enter');
      await expect(page.getByRole('button', { name: 'Comprobar conexión' })).toBeEnabled();
      await page.evaluate(() => window.scrollTo(0, 0));
      await page.screenshot({ path: testInfo.outputPath('screen.png'), fullPage: true });
    });
  }

  test(`Kipo conserva el último estado si falla la red en ${port}`, async ({ page }) => {
    await page.goto(`https://localhost:${port}`);
    await expect(page.getByText('Kipo: conexión establecida con el servicio v1.')).toBeVisible();
    await page.route('**/api/v1/status', (route) => route.abort());
    await page.getByRole('button', { name: 'Comprobar conexión' }).click();
    await expect(page.getByRole('alert')).toContainText('No se pudo conectar con Kipo.');
    await expect(page.getByText('Kipo: conexión establecida con el servicio v1.')).toBeVisible();
  });
}
