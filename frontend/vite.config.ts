/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

// The dev server is the SAME ORIGIN as the API: there is no CORS anywhere in the
// backend (foundation §7 #27), so the browser must reach every service through this
// proxy. changeOrigin + an empty cookieDomainRewrite let the refresh cookie survive.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const AUTH = env.AUTH_ORIGIN ?? 'http://localhost:5005';
  const USER = env.USER_ORIGIN ?? 'http://localhost:5079';
  const QUIZ = env.QUIZ_ORIGIN ?? 'http://localhost:5224';

  const forward = (target: string) => ({
    target,
    changeOrigin: true,
    secure: false,
    cookieDomainRewrite: '',
  });

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: { '@': path.resolve(__dirname, 'src') },
    },
    server: {
      // design-system/ is a sibling of frontend/, imported by src/styles/tailwind.css.
      fs: { allow: ['..'] },
      proxy: {
        // Specific prefixes only, never an /api catch-all, or auth calls reach the wrong service.
        '/api/auth': forward(AUTH),
        '/api/profile': forward(USER),
        '/api/classrooms': forward(QUIZ),
        '/api/quizzes': forward(QUIZ),
        '/api/attempts': forward(QUIZ),
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
