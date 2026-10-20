import { keycloak, expireSession } from '../auth/session';
export class ApiError extends Error {
    constructor(
        public status: number,
        message: string,
        public errors: Record<string, string[]> = {},
    ) {
        super(message);
    }
}
export async function api<T>(
    path: string,
    options: RequestInit = {},
): Promise<T> {
    try {
        await keycloak.updateToken(30);
    } catch {
        expireSession();
        throw new ApiError(401, 'Your session ended. Sign in again.');
    }
    const headers = new Headers(options.headers);
    headers.set('Authorization', `Bearer ${keycloak.token}`);
    if (options.body) headers.set('Content-Type', 'application/json');
    const response = await fetch(`/api${path}`, { ...options, headers });
    if (!response.ok) {
        const problem = await response.json().catch(() => ({}));
        if (response.status === 401) expireSession();
        throw new ApiError(
            response.status,
            problem.detail ||
                problem.title ||
                `Request failed (${response.status}).`,
            problem.errors || {},
        );
    }
    return response.status === 204 ? (undefined as T) : response.json();
}
export const body = (value: unknown) => JSON.stringify(value);
