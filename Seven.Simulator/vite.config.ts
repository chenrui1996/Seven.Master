import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

/** 开发默认代理到 Seven.WebApi */
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/hub': {
        target: 'http://localhost:5000',
        ws: true,
        changeOrigin: true,
      },
    },
  },
})
