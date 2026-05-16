import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: 'https://localhost:7256', changeOrigin: true, secure: false },
      '/hubs': { target: 'https://localhost:7256', changeOrigin: true, ws: true, secure: false },
    },
  },
  build: {
    outDir: '../PrivateInsta.Api/wwwroot',
    emptyOutDir: true,
  },
});
