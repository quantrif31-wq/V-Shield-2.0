import { defineConfig } from '@playwright/test'
import { uatEnvironment } from './uat/helpers/environment.js'

export default defineConfig({
  testDir: './uat',
  outputDir: './uat-results',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: true,
  timeout: 45_000,
  reporter: [['list'], ['html', { outputFolder: 'uat-report', open: 'never' }]],
  use: {
    baseURL: uatEnvironment.frontendUrl,
    locale: 'vi-VN',
    timezoneId: 'Asia/Ho_Chi_Minh',
    trace: 'off',
    screenshot: 'off',
    video: 'off',
    ignoreHTTPSErrors: uatEnvironment.ignoreHttpsErrors,
  },
})
