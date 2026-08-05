import { test, expect } from './fixtures'

const matrix = [
  ['Admin', '/employees', 'Hồ sơ nhân viên'],
  ['BaoVe', '/vehicles', 'Quản lý phương tiện'],
  ['QuanLy', '/access-logs', 'Lịch sử ra vào'],
  ['LeTan', '/guest-profiles', 'Quản lý khách'],
  ['NhanSu', '/employees', 'Hồ sơ nhân viên'],
]

for (const [role, path, heading] of matrix) {
  test.describe(`${role} authenticated fixture`, () => {
    test.use({ role })
    test(`opens authorized ${path}`, async ({ page }) => {
      await page.goto(path)
      await expect(page.locator('h1')).toContainText(heading)
      await expect(page).not.toHaveURL(/\/login/)
    })
  })
}
