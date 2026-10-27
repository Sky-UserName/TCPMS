import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    strictPort: false,
    // 开发时把 /api 代理到本地后端，前端代码里不再写死后端地址。
    proxy: {
      '/api': {
        target: 'http://localhost:5180',
        changeOrigin: true
      },
      '/static': {
        target: 'http://localhost:5180',
        changeOrigin: true
      },
      '/uploads': {
        target: 'http://localhost:5180',
        changeOrigin: true
      }
    }
  }
})
