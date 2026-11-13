/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true, // porta fixa: é a origem liberada no CORS da API
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Mesma origem da API de propósito: o jsdom aplica CORS ao XHR, e com withCredentials uma
    // resposta do MSW sem cabeçalhos Access-Control-* chegaria ao axios como "Network Error".
    environmentOptions: {
      jsdom: { url: 'http://localhost:3000' },
    },
    // O Vitest roda em mode=test, que não lê .env.development — e o http.ts lança no boot sem a URL.
    env: {
      VITE_API_URL: 'http://localhost:3000/api/v1',
      // A API grava e devolve hora de Brasília (docs/contexto-web.md §10); sem isto, os testes de
      // data mudariam de resultado entre a máquina de dev e o CI, que roda em UTC.
      TZ: 'America/Sao_Paulo',
    },
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'lcov'],
      // Sem o include, só entram no cálculo os arquivos que algum teste importa.
      include: ['src/**'],
      exclude: ['src/main.tsx', 'src/api/schema.d.ts', 'src/vite-env.d.ts', 'src/test/**'],
      // Piso do TCC (linha Web Apps): abaixo disto `npm run test:cov` sai com erro e o job `web` do CI reprova.
      thresholds: { lines: 50 },
    },
  },
})
