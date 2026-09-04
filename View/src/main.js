import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router'
import { installObservability } from './services/observability'
import { installRouteProgress } from './services/routeLoading'

// Khi một lazy-loaded chunk không tải được (mạng chập chờn / index.html cũ sau
// rebuild), Vite phát sự kiện vite:preloadError. Nếu không xử lý, route hiện tại
// render rỗng -> white screen cho tới khi reload thủ công. Tự reload an toàn.
window.addEventListener('vite:preloadError', (event) => {
  event.preventDefault()
  const target = window.location.pathname + window.location.search
  const key = 'vshield:preload-reload'
  const raw = sessionStorage.getItem(key)
  let previous = null
  try { previous = raw ? JSON.parse(raw) : null } catch { previous = raw ? { target: raw, at: 0 } : null }
  if (previous?.target === target && Date.now() - Number(previous.at || 0) < 30_000) {
    return
  }
  sessionStorage.setItem(key, JSON.stringify({ target, at: Date.now() }))
  window.location.assign(target)
})

const app = createApp(App)
app.use(router)
installObservability(app, router)
installRouteProgress(router)
app.mount('#app')
