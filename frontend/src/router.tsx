import { createBrowserRouter } from 'react-router';
import { routes } from './routes';

// Data router used for routing only (nested routes, guards). Loaders and actions
// stay unused: TanStack Query owns all server state (spec 0001, foundation §7 #25).
// The route table lives in routes.tsx so the build time prerender can share it
// without triggering this browser router (spec 0003).
export const router = createBrowserRouter(routes);
