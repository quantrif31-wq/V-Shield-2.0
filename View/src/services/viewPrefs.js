/**
 * Bộ nhớ thiết lập giao diện theo từng view + từng user (UI prefs).
 *
 * - Lưu vào localStorage với key theo userId, nên mỗi tài khoản có riêng bộ nhớ
 *   và khi đúng người đó quay lại (dù tắt app, đăng xuất, chuyển view) thì
 *   setup của họ được khôi phục lại.
 * - CHỈ lưu các giá trị "setup để làm việc" (lựa chọn cổng, camera, bộ lọc...),
 *   KHÔNG lưu dữ liệu vận hành cần lưu database.
 * - Khi khôi phục KHÔNG tự kích hoạt tính năng (không tự bật stream/quét).
 */
const STORAGE_PREFIX = 'vshield.viewPrefs.v1'
const AUTH_USER_KEY = 'v_shield_user'

function currentUserId() {
  try {
    const raw = sessionStorage.getItem(AUTH_USER_KEY) || localStorage.getItem(AUTH_USER_KEY)
    const user = raw ? JSON.parse(raw) : null
    return (user && (user.userId || user.username)) || 'anonymous'
  } catch {
    return 'anonymous'
  }
}

function storageKey(viewKey) {
  return `${STORAGE_PREFIX}.${currentUserId()}.${viewKey}`
}

/**
 * Đọc setup đã lưu của user hiện tại cho 1 view.
 * @param {string} viewKey
 * @returns {object|null}
 */
export function loadViewPrefs(viewKey) {
  try {
    const raw = localStorage.getItem(storageKey(viewKey))
    if (!raw) return null
    const parsed = JSON.parse(raw)
    return parsed && typeof parsed === 'object' ? parsed.data || null : null
  } catch {
    return null
  }
}

/**
 * Ghi setup của user hiện tại cho 1 view (merge theo key).
 * @param {string} viewKey
 * @param {object} data
 */
export function saveViewPrefs(viewKey, data) {
  if (!data || typeof data !== 'object') return
  try {
    localStorage.setItem(storageKey(viewKey), JSON.stringify({
      version: 1,
      savedAt: Date.now(),
      data,
    }))
  } catch {
    // localStorage không khả dụng / hết quota — bỏ qua, không làm hỏng luồng chính
  }
}

/** Xoá setup đã lưu của user hiện tại cho 1 view. */
export function clearViewPrefs(viewKey) {
  try {
    localStorage.removeItem(storageKey(viewKey))
  } catch {}
}

/** Xoá toàn bộ view-prefs của user hiện tại. */
export function clearAllUserViewPrefs() {
  try {
    const prefix = `${STORAGE_PREFIX}.${currentUserId()}.`
    const keysToRemove = []
    for (let i = 0; i < localStorage.length; i += 1) {
      const key = localStorage.key(i)
      if (key && key.startsWith(prefix)) keysToRemove.push(key)
    }
    keysToRemove.forEach((key) => localStorage.removeItem(key))
  } catch {}
}
