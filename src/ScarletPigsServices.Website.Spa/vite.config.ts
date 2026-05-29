import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const port = Number(env.PORT || 5173)
  const apiTarget = env.API_HTTPS || env.API_HTTP

  return {
    plugins: [react()],
    server: {
      host: true,
      port,
      strictPort: true,
      proxy: apiTarget
        ? {
            '/events': {
              target: apiTarget,
              changeOrigin: true,
              secure: false,
            },
            '/files': {
              target: apiTarget,
              changeOrigin: true,
              secure: false,
            },
            '/users': {
              target: apiTarget,
              changeOrigin: true,
              secure: false,
            },
            '/workshop': {
              target: apiTarget,
              changeOrigin: true,
              secure: false,
            },
          }
        : undefined,
    },
    preview: {
      host: true,
      port,
      strictPort: true,
    },
  }
})
