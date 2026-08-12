import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router'
import { installObservability } from './services/observability'
import { installRouteProgress } from './services/routeLoading'

const app = createApp(App)
app.use(router)
installObservability(app, router)
installRouteProgress(router)
app.mount('#app')
