import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: '0.0.0.0', // Allow connections from outside container
    proxy: {
      '/api': {
        // Use backend container name when in Docker, localhost when running locally
        target: process.env.NODE_ENV === 'development' && process.env.DOCKER_CONTAINER
          ? 'http://backend:8080'
          : 'http://localhost:5001',
        changeOrigin: true,
        secure: false,
      }
    }
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  }
})
