import { apiFetch } from '@/lib/api/client';
import { authSessionSchema } from '@/lib/auth/session';
import type { AuthSession, Role } from '@/lib/auth/session';

// All three are anonymous / cookie-authed: skipAuth so no bearer is attached and
// a 401 does not trigger a refresh loop. The server sets the refresh cookie.

export function login(email: string, password: string): Promise<AuthSession> {
  return apiFetch('/api/auth/login', {
    method: 'POST',
    json: { email, password },
    schema: authSessionSchema,
    skipAuth: true,
  });
}

export function register(email: string, password: string, role: Role): Promise<AuthSession> {
  return apiFetch('/api/auth/register', {
    method: 'POST',
    json: { email, password, role },
    schema: authSessionSchema,
    skipAuth: true,
  });
}

export function logout(): Promise<unknown> {
  return apiFetch('/api/auth/logout', { method: 'POST', skipAuth: true });
}
