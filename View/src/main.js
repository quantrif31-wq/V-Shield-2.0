import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router'
import { installObservability } from './services/observability'

const app = createApp(App)
app.use(router)
installObservability(app, router)
app.mount('#app')
