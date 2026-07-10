type Listener = (token: string | null) => void;

// The access token lives ONLY here, in memory. Never localStorage/sessionStorage:
// a script on the page must not be able to read a durable credential. The durable
// half of the session is the HttpOnly refresh cookie, which JS cannot read at all.
let accessToken: string | null = null;
const listeners = new Set<Listener>();

export const tokenStore = {
  get(): string | null {
    return accessToken;
  },
  set(token: string | null): void {
    accessToken = token;
    for (const listener of listeners) listener(token);
  },
  clear(): void {
    tokenStore.set(null);
  },
  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
};
