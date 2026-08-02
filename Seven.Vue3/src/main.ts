import { createApp } from 'vue'
import { createPinia } from 'pinia'
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/dark/css-vars.css'
import * as ElementPlusIconsVue from '@element-plus/icons-vue'
import App from './App.vue'
import router from './router'
import { setupDirectives } from './directives/permission'
import './styles/theme.css'
import i18n from './locales'
import { useThemeStore } from './stores/theme'
import { useLocaleStore } from './stores/locale'
import { useFeatureStore } from './stores/features'

const app = createApp(App)
const pinia = createPinia()
pinia.use(piniaPluginPersistedstate)

app.use(pinia)
app.use(i18n)
useThemeStore(pinia).init()
useLocaleStore(pinia).init()

const featureStore = useFeatureStore(pinia)
await featureStore.load()

app.use(router)
app.use(ElementPlus)
setupDirectives(app)

for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component)
}

app.mount('#app')
