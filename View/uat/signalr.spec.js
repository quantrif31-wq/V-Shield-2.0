import { expect, test } from '@playwright/test'
import { loginViaUi } from './helpers/auth.js'

test('chat SignalR reconnects, remounts and opens a second tab without a connection storm', async ({ page, context }, testInfo) => {
  const lifecycle = []
  await context.exposeBinding('recordSignalLifecycle', (_source, event) => lifecycle.push({ name: event.name, timestamp: Date.now() }))
  await page.addInitScript(() => window.addEventListener('vshield:observability', event => {
    if (String(event.detail?.name || '').startsWith('signalr_')) window.recordSignalLifecycle({ name: event.detail.name })
  }))
  await loginViaUi(page, 'Admin')
  await page.goto('/chat')
  const status = page.getByRole('status')
  await expect(status).toContainText('Live', { timeout: 15_000 })
  const priorConversationCount = await page.locator('.conversation-item').count()
  const disruptedAt = Date.now()
  await context.setOffline(true)
  await expect(status).toContainText(/kết nối lại|cũ|ngắt kết nối/i, { timeout: 20_000 })
  expect(await page.locator('.conversation-item').count()).toBe(priorConversationCount)
  await context.setOffline(false)
  await expect(status).toContainText('Live', { timeout: 45_000 })
  const recoveredMs = Date.now() - disruptedAt

  await page.goto('/employees')
  await page.goto('/chat')
  await expect(page.getByRole('status')).toContainText('Live', { timeout: 15_000 })

  const secondTab = await context.newPage()
  await secondTab.addInitScript(() => window.addEventListener('vshield:observability', event => {
    if (String(event.detail?.name || '').startsWith('signalr_')) window.recordSignalLifecycle({ name: event.detail.name })
  }))
  await loginViaUi(secondTab, 'Admin')
  await secondTab.goto('/chat')
  await expect(secondTab.getByRole('status')).toContainText('Live', { timeout: 15_000 })
  await secondTab.close()

  const summary = {
    recoveredMs,
    connectedEvents: lifecycle.filter(event => event.name === 'signalr_connected').length,
    reconnectEvents: lifecycle.filter(event => event.name === 'signalr_reconnected').length,
    disconnectEvents: lifecycle.filter(event => event.name === 'signalr_disconnected').length,
  }
  await testInfo.attach('signalr-disruption-summary.json', { body: Buffer.from(JSON.stringify(summary, null, 2)), contentType: 'application/json' })
  expect(summary.connectedEvents).toBeLessThanOrEqual(4)
  expect(recoveredMs).toBeLessThan(45_000)
})
