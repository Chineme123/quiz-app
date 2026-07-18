/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

// The dev server is the SAME ORIGIN as the API: there is no CORS anywhere in the
// backend (foundation §7 #27), so the browser reaches the API through this proxy.
// One target now: the single Quiztin.Api host (spec 0007), which owns /api and serves
// the SPA. This mirrors production (the SPA and the API share the one host origin).
// changeOrigin + an empty cookieDomainRewrite let the refresh cookie survive.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const API_ORIGIN = env.API_ORIGIN ?? 'http://localhost:8080';

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: { '@': path.resolve(__dirname, 'src') },
    },
    // Emit a build manifest so the prerender script can find the landing chunk's
    // CSS and JS and link them from the prerendered "/" document (spec 0003, AC-13):
    // the landing is a separate chunk, so its styles must be linked up front or the
    // prerendered page paints unstyled until the chunk loads.
    build: { manifest: true },
    server: {
      // design-system/ is a sibling of frontend/, imported by src/styles/tailwind.css.
      fs: { allow: ['..'] },
      proxy: {
        // Everything under /api goes to the single host.
        '/api': {
          target: API_ORIGIN,
          changeOrigin: true,
          secure: false,
          cookieDomainRewrite: '',
        },
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
      css: true,
    },
  };
});
