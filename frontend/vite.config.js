import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// En desarrollo, Vite reenvía /api a la API (docker compose o dotnet run) y no hace falta CORS.
// En producción, el resultado se escribe en el wwwroot de la API, que lo sirve en el mismo puerto.
const urlApi = process.env.VITE_URL_API ?? 'http://localhost:5003';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: urlApi, changeOrigin: true },
    },
  },
  build: {
    outDir: '../src/Api/RastreadorHabitos.Api/wwwroot',
    emptyOutDir: true,
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/pruebas/configuracion.js',
    css: false,
  },
});
