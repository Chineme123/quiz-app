# Quiztin frontend

React + Vite + TypeScript SPA. Styled from the design tokens in `../design-system/` via Tailwind v4. Spec: [`docs/specs/0001-frontend-foundation/`](../docs/specs/0001-frontend-foundation/index.md).

## Run it

```bash
npm install
npm run dev        # http://localhost:5173
```

The dev server proxies the API same-origin (there is no CORS in the backend). Start the services on their http launch profiles first, or override the targets:

```bash
cp .env.example .env   # then edit if your ports differ
```

Default proxy targets: Auth `5005`, User `5079`, Quiz `5224`. The refresh cookie rides through the proxy (`changeOrigin` + `cookieDomainRewrite`).

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Dev server with the API proxy |
| `npm run build` | Typecheck (`tsc --noEmit`) then production build |
| `npm run lint` | ESLint (type-aware, plus `jsx-a11y`) |
| `npm test` | Vitest run |
| `npm run format` | Prettier write |

## Shape

```
src/
  app/          Providers (Query, Auth, Toast)
  components/ui/ The design-system primitives (Icon, Button, Field, TextField, Select, Card, Toast)
  features/     Screen-specific code (auth, profile)
  layout/       App shell, header, profile menu, route guard
  lib/
    api/        Typed fetch client, refresh, query client
    auth/       In-memory token store, session schema, auth context
  styles/       tailwind.css (tokens bound via @theme inline; Preflight off)
```

## Notes

- The access token is held in memory only; the durable credential is an `HttpOnly` refresh cookie. See the spec.
- Tailwind Preflight is deliberately not loaded (the design system's `base.css` is the reset). A `border-style: solid` rule is restored in `styles/tailwind.css`.
- Not in `QuizApp.sln` and not yet in CI: `dotnet` CI does not lint or test this app.
