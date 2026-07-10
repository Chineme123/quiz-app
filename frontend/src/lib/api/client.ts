import type { ZodType } from 'zod';
import { tokenStore } from '@/lib/auth/tokenStore';
import { refreshSession } from './refresh';
import { ApiError, AuthError, ContractError, NetworkError, ValidationError } from './errors';

export interface ApiFetchOptions<T> extends Omit<RequestInit, 'body'> {
  /** JSON body; stringified and given a Content-Type automatically. */
  json?: unknown;
  body?: BodyInit | null;
  /** Validate the 2xx body at the boundary; a mismatch throws ContractError. */
  schema?: ZodType<T>;
  /** Auth endpoints: do not attach a bearer token and do not attempt a refresh. */
  skipAuth?: boolean;
  /** Internal: set on the single automatic retry after a silent refresh. */
  _retried?: boolean;
}

/**
 * Typed fetch wrapper. Attaches the in-memory access token, and on a 401 it
 * silently refreshes once and retries exactly once before giving up. Always
 * sends credentials so the refresh cookie rides same-origin auth calls.
 */
export async function apiFetch<T = unknown>(path: string, options: ApiFetchOptions<T> = {}): Promise<T> {
  const { json, body, schema, skipAuth = false, _retried = false, headers, ...rest } = options;

  const finalHeaders = new Headers(headers);
  let finalBody = body ?? null;
  if (json !== undefined) {
    finalBody = JSON.stringify(json);
    if (!finalHeaders.has('Content-Type')) finalHeaders.set('Content-Type', 'application/json');
  }

  const token = tokenStore.get();
  if (token && !skipAuth) finalHeaders.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(path, { ...rest, headers: finalHeaders, body: finalBody, credentials: 'include' });
  } catch (cause) {
    throw new NetworkError(cause);
  }

  if (res.status === 401 && !skipAuth && !_retried) {
    const session = await refreshSession();
    if (session) return apiFetch<T>(path, { ...options, _retried: true });
    throw new AuthError();
  }

  if (!res.ok) {
    const errorBody = await parseBody(res);
    if ((res.status === 400 || res.status === 422) && isStringArray(errorBody)) {
      throw new ValidationError(res.status, errorBody);
    }
    throw new ApiError(res.status, errorBody);
  }

  const data = await parseBody(res);
  if (schema) {
    const parsed = schema.safeParse(data);
    if (!parsed.success) throw new ContractError(path, parsed.error);
    return parsed.data;
  }
  return data as T;
}

async function parseBody(res: Response): Promise<unknown> {
  if (res.status === 204 || res.headers.get('Content-Length') === '0') return null;
  const text = await res.text();
  if (!text) return null;
  const type = res.headers.get('Content-Type') ?? '';
  if (type.includes('application/json')) {
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }
  return text;
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((v) => typeof v === 'string');
}
