import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '../../tests/e2e',
  fullyParallel: true,
  use: {
    browserName: 'chromium',
    headless: true,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command:
        'dotnet run --project ../../src/Platform.Api --no-launch-profile --urls http://127.0.0.1:5080',
      url: 'http://127.0.0.1:5080/api/v1/health/live',
      reuseExistingServer: !process.env['CI'],
    },
    {
      command: 'npm run start:manager:https',
      url: 'https://localhost:4200',
      ignoreHTTPSErrors: true,
      reuseExistingServer: !process.env['CI'],
    },
    {
      command: 'npm run start:admin:https',
      url: 'https://localhost:4300',
      ignoreHTTPSErrors: true,
      reuseExistingServer: !process.env['CI'],
    },
  ],
});
