import { tokenStore } from '@/lib/auth/tokenStore';
import { authSessionSchema } from '@/lib/auth/session';
import type { AuthSession } from '@/lib/auth/session';

// Cross-tab coordination. When one tab refreshes, the others adopt the new token
// instead of each racing to rotate the same cookie (which the backend's reuse
// detector would read as theft). BroadcastChannel is absent in jsdom, hence the guard.
const channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('quiztin-auth') : null;

channel?.addEventListener('message', (event: MessageEvent) => {
  const data = event.data as { type?: string; token?: string } | null;
  if (data?.type === 'token' && typeof data.token === 'string') {
    tokenStore.set(data.token);
  } else if (data?.type === 'signout') {
    tokenStore.clear();
  }
});

// Single-flight: concurrent callers share one in-flight refresh request.
let inFlight: Promise<AuthSession | null> | null = null;

/**
 * Exchange the HttpOnly refresh cookie for a new access token. Returns the
 * session on success, or null when the cookie is missing/expired/revoked.
 * Used both on app boot and by the API client's 401 handler.
 */
export function refreshSession(): Promise<AuthSession | null> {
  inFlight ??= doRefresh().finally(() => {
    inFlight = null;
  });
  return inFlight;
}

async function doRefresh(): Promise<AuthSession | null> {
  let res: Response;
  try {
    res = await fetch('/api/auth/refresh', { method: 'POST', credentials: 'include' });
  } catch {
    return null;
  }

  if (!res.ok) {
    tokenStore.clear();
    channel?.postMessage({ type: 'signout' });
    return null;
  }

  const parsed = authSessionSchema.safeParse(await res.json());
  if (!parsed.success) {
    tokenStore.clear();
    return null;
  }

  tokenStore.set(parsed.data.token);
  channel?.postMessage({ type: 'token', token: parsed.data.token });
  return parsed.data;
}

/** Broadcast a sign-out so every tab drops the session together. */
export function broadcastSignOut(): void {
  channel?.postMessage({ type: 'signout' });
}

/** Broadcast a freshly-issued token (after login/register) to other tabs. */
export function broadcastToken(token: string): void {
  channel?.postMessage({ type: 'token', token });
}
