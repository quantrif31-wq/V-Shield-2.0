import AxeBuilder from '@axe-core/playwright'
import { test, expect } from './fixtures'

const routes = ['/employees', '/pre-registrations', '/vehicles', '/access-logs', '/device-management', '/watchlist', '/ai-review-queue', '/redaction-queue', '/operations-dashboard']

for (const path of routes) {
  test(`axe baseline ${path}`, async ({ page }, testInfo) => {
    await page.goto(path)
    await expect(page.locator('h1')).toBeVisible()
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze()
    await testInfo.attach(`axe-${path.slice(1)}.json`, { body: JSON.stringify(result.violations, null, 2), contentType: 'application/json' })
    const blocking = result.violations.filter((item) => ['critical', 'serious'].includes(item.impact))
    expect(blocking, blocking.map((item) => `${item.id}: ${item.help} (${item.nodes.length}) — ${item.nodes.map((node) => node.target.join(' > ')).join('; ')}`).join('\n')).toEqual([])
  })
}

test('keyboard navigation, modal focus, validation, sorting and live feedback', async ({ page }) => {
  await page.goto('/employees')
  await expect(page.locator('h1')).toBeVisible()

  await page.keyboard.press('Tab')
  const skipLink = page.getByRole('link', { name: 'Bỏ qua điều hướng' })
  await expect(skipLink).toBeFocused()
  await page.keyboard.press('Enter')
  await expect(page.locator('#main-content')).toBeFocused()

  const createTrigger = page.getByRole('button', { name: 'Thêm nhân viên' }).first()
  await createTrigger.focus()
  await page.keyboard.press('Enter')
  const dialog = page.getByRole('dialog')
  const closeButton = dialog.getByRole('button', { name: 'Đóng hộp thoại' })
  const submitButton = dialog.getByRole('button', { name: 'Tạo nhân viên' })
  await expect(closeButton).toBeFocused()
  await page.keyboard.press('Shift+Tab')
  await expect(submitButton).toBeFocused()
  await page.keyboard.press('Tab')
  await expect(closeButton).toBeFocused()

  const modalAxe = await new AxeBuilder({ page }).include('.vs-modal-layer').withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze()
  expect(modalAxe.violations.filter((item) => ['critical', 'serious'].includes(item.impact))).toEqual([])

  await page.keyboard.press('Escape')
  await expect(dialog).toBeHidden()
  await expect(createTrigger).toBeFocused()

  await createTrigger.click()
  await dialog.getByRole('button', { name: 'Tạo nhân viên' }).focus()
  await page.keyboard.press('Enter')
  await expect(dialog.getByLabel('Họ và tên')).toHaveAttribute('aria-invalid', 'true')
  await page.keyboard.press('Escape')

  const sortButton = page.locator('.vs-table-wrap th').filter({ hasText: 'Nhân viên' }).getByRole('button')
  await sortButton.focus()
  await page.keyboard.press('Enter')
  await expect(sortButton.locator('xpath=ancestor::th')).toHaveAttribute('aria-sort', 'descending')

  await page.goto('/pre-registrations')
  await page.getByRole('button', { name: 'Duyệt', exact: true }).click()
  await expect(page.locator('.vs-toast[role="status"]')).toContainText('Đã duyệt')
})
