import { expect, test } from '@playwright/test'
import { loginViaUi } from './helpers/auth.js'
import { uatEnvironment } from './helpers/environment.js'

test('post-deploy Admin smoke covers assets, migrated routes, realtime and logout', async ({ page }) => {
  const severe = []
  page.on('console', message => { if (message.type() === 'error') severe.push(message.text()) })
  const responses = []
  page.on('response', response => { if (['stylesheet', 'font', 'script'].includes(response.request().resourceType())) responses.push(response) })
  await loginViaUi(page, 'Admin')
  expect(await page.locator('meta[name="v-shield-version"]').getAttribute('content')).toBe(uatEnvironment.expectedVersion)
  for (const route of ['/', '/employees', '/pre-registrations', '/vehicles', '/access-logs', '/device-management', '/watchlist', '/ai-review-queue', '/redaction-queue', '/operations-dashboard']) {
    await page.goto(route)
    await expect(page.locator('main, .main-content').first()).toBeVisible()
  }
  await page.goto('/chat')
  await expect(page.getByRole('status')).toContainText(/Live/i, { timeout: 15_000 })
  expect(responses.filter(response => response.status() >= 400).map(response => response.url())).toEqual([])
  expect(severe).toEqual([])
  const logout = page.getByRole('button', { name: /đăng xuất/i })
  await logout.click()
  await expect(page).toHaveURL(/\/login/)
})
