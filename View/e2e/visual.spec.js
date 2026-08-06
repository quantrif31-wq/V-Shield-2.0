import { test, expect } from './fixtures'

const coreRoutes = [
  ['employees', '/employees'], ['visitors', '/pre-registrations'], ['vehicles', '/vehicles'],
  ['access-logs', '/access-logs'], ['devices', '/device-management'], ['watchlist', '/watchlist'],
  ['ai-review', '/ai-review-queue'], ['redaction', '/redaction-queue'], ['operations', '/operations-dashboard'],
]

for (const [name, path] of coreRoutes) {
  test(`${name} visual baseline`, async ({ page }) => {
    const errors = []; page.on('pageerror', (error) => errors.push(error.message))
    await page.goto(path)
    await expect(page.locator('h1')).toBeVisible()
    await page.waitForTimeout(250)
    await expect(page).toHaveScreenshot(`${name}.png`, { fullPage: true })
    expect(errors).toEqual([])
  })
}

test.describe('important empty state', () => {
  test.use({ mockState: 'empty' })
  test('employees empty visual baseline', async ({ page }) => { await page.goto('/employees'); await expect(page.getByText('Không có nhân viên phù hợp')).toBeVisible(); await expect(page).toHaveScreenshot('employees-empty.png', { fullPage: true }) })
})

test.describe('important error state', () => {
  test.use({ mockState: 'error' })
  test('employees error visual baseline', async ({ page }) => { await page.goto('/employees'); await expect(page.getByRole('heading', { name: 'Không thể tải dữ liệu' })).toBeVisible(); await expect(page).toHaveScreenshot('employees-error.png', { fullPage: true }) })
})

async function openAndWait(page, path, heading) {
  await page.goto(path)
  await expect(page.locator('h1').filter({ hasText: heading })).toBeVisible()
}

test('navigation responsive state', async ({ page }) => {
  await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
  await page.getByRole('button', { name: 'Mở điều hướng' }).click()
  if (page.viewportSize().width <= 768) await expect(page.locator('.sidebar-mobile-close')).toBeVisible()
  await expect(page).toHaveScreenshot('navigation-responsive.png', { fullPage: true })
})

test('employees create form modal', async ({ page }) => {
  await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
  await page.getByRole('button', { name: 'Thêm nhân viên' }).first().click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page).toHaveScreenshot('employees-create-form.png', { fullPage: true })
})

test('employees edit detail modal', async ({ page }) => {
  await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
  await page.getByRole('button', { name: 'Sửa' }).first().click()
  await expect(page.getByRole('heading', { name: 'Cập nhật nhân viên' })).toBeVisible()
  await expect(page).toHaveScreenshot('employees-detail-edit.png', { fullPage: true })
})

test('employees destructive confirmation', async ({ page }) => {
  await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
  await page.getByRole('button', { name: 'Xóa' }).first().click()
  await expect(page.getByRole('heading', { name: 'Xóa hồ sơ nhân viên?' })).toBeVisible()
  await expect(page).toHaveScreenshot('modal-destructive.png', { fullPage: true })
})

test('visitors detail modal', async ({ page }) => {
  await openAndWait(page, '/pre-registrations', 'Đăng ký khách trước')
  await page.getByRole('button', { name: 'Chi tiết' }).first().click()
  await expect(page.getByRole('heading', { name: /Chi tiết đăng ký/ })).toBeVisible()
  await expect(page).toHaveScreenshot('visitors-detail.png', { fullPage: true })
})

test('visitors create form modal', async ({ page }) => {
  await openAndWait(page, '/pre-registrations', 'Đăng ký khách trước')
  await page.getByRole('button', { name: 'Tạo link đăng ký' }).first().click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page).toHaveScreenshot('visitors-create-form.png', { fullPage: true })
})

test('vehicles create form modal', async ({ page }) => {
  await openAndWait(page, '/vehicles', 'Quản lý phương tiện')
  await page.getByRole('button', { name: 'Đăng ký phương tiện' }).first().click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page).toHaveScreenshot('vehicles-create-form.png', { fullPage: true })
})

test('vehicles edit detail modal', async ({ page }) => {
  await openAndWait(page, '/vehicles', 'Quản lý phương tiện')
  await page.getByRole('button', { name: 'Sửa' }).first().click()
  await expect(page.getByRole('heading', { name: 'Cập nhật phương tiện' })).toBeVisible()
  await expect(page).toHaveScreenshot('vehicles-detail-edit.png', { fullPage: true })
})

test('device configuration modal and disconnected telemetry', async ({ page }) => {
  await openAndWait(page, '/device-management', 'Quản lý camera & cổng')
  await expect(page.getByText('Mất kết nối')).toBeVisible()
  await page.getByRole('button', { name: 'Thêm camera' }).first().click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page).toHaveScreenshot('device-form-disconnected.png', { fullPage: true })
})

test('dark mode visual baseline', async ({ page }) => {
  await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
  await page.getByRole('button', { name: 'Chuyển sang chế độ phòng điều khiển tối' }).click()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
  await expect(page).toHaveScreenshot('dark-mode.png', { fullPage: true })
})

test('compact mode visual baseline', async ({ page }) => {
  await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
  await page.locator('.header-user').click()
  await page.getByRole('button', { name: 'Gọn', exact: true }).click()
  await expect(page.locator('html')).toHaveAttribute('data-density', 'compact')
  await expect(page).toHaveScreenshot('compact-mode.png', { fullPage: true })
})

test.describe('permission denied state', () => {
  test.use({ mockState: 'forbidden' })
  test('employees permission visual baseline', async ({ page }) => {
    await openAndWait(page, '/employees', 'Hồ sơ nhân viên')
    await expect(page.getByText('Bạn không có quyền xem dữ liệu này')).toBeVisible()
    await expect(page).toHaveScreenshot('employees-permission-denied.png', { fullPage: true })
  })
})

test.describe('loading skeleton state', () => {
  test.use({ mockState: 'loading' })
  test('employees loading visual baseline', async ({ page }) => {
    await page.goto('/employees')
    await expect(page.locator('h1').filter({ hasText: 'Hồ sơ nhân viên' })).toBeVisible()
    await expect(page.locator('[aria-busy="true"]').first()).toBeVisible()
    await expect(page).toHaveScreenshot('employees-loading.png', { fullPage: true })
  })
})
