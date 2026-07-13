/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

// The dev server is the SAME ORIGIN as the API: there is no CORS anywhere in the
// backend (foundation §7 #27), so the browser reaches the API through this proxy.
// One target now: the YARP gateway (spec 0002), which does the /api routing to the
// services. This mirrors production (the SPA and the API share the gateway origin).
// changeOrigin + an empty cookieDomainRewrite let the refresh cookie survive.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const GATEWAY = env.GATEWAY_ORIGIN ?? 'http://localhost:8080';

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: { '@': path.resolve(__dirname, 'src') },
    },
    server: {
      // design-system/ is a sibling of frontend/, imported by src/styles/tailwind.css.
      fs: { allow: ['..'] },
      proxy: {
        // Everything under /api goes to the gateway, which routes to the right service.
        '/api': {
          target: GATEWAY,
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
