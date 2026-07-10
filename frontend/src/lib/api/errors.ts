/** Non-2xx response the caller may still want to inspect (status + parsed body). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly body: unknown,
    message?: string,
  ) {
    super(message ?? `Request failed with status ${status}`);
    this.name = 'ApiError';
  }
}

/**
 * A 400/422 whose body is a bare array of strings — the shape UserService's
 * PUT /api/profile returns. Features map these back to fields by message prefix.
 */
export class ValidationError extends ApiError {
  constructor(
    status: number,
    readonly errors: string[],
  ) {
    super(status, errors, 'Validation failed');
    this.name = 'ValidationError';
  }
}

/** The request never reached the server (offline, DNS, CORS). */
export class NetworkError extends Error {
  constructor(readonly cause?: unknown) {
    super('Could not reach the server. Check your connection and try again.');
    this.name = 'NetworkError';
  }
}

/** A 2xx body that failed its Zod schema: the backend contract drifted. */
export class ContractError extends Error {
  constructor(
    readonly path: string,
    readonly issues: unknown,
  ) {
    super(`Response from ${path} did not match its expected shape.`);
    this.name = 'ContractError';
  }
}

/** Authentication is gone and could not be refreshed: the user is signed out. */
export class AuthError extends Error {
  constructor() {
    super('Your session has ended. Please sign in again.');
    this.name = 'AuthError';
  }
}
