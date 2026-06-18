import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// In dev proxyt Vite /api naar de ASP.NET Core API (http). Zo blijft de SPA
// CORS-vrij en gebruiken we relatieve /api-paden. Pas de poort aan op je
// launchSettings (standaard hier 5181).
const API_DOEL = process.env.VITE_API_URL ?? 'http://localhost:5181';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: API_DOEL, changeOrigin: true },
    },
  },
});
