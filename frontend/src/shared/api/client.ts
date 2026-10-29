import { accessToken, expireSession } from '../auth/session';
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
    let token: string;
    try {
        token = await accessToken();
    } catch {
        throw new ApiError(401, 'Your session ended. Sign in again.');
    }
    const headers = new Headers(options.headers);
    if (options.body) headers.set('Content-Type', 'application/json');
    const send = () => {
        headers.set('Authorization', `Bearer ${token}`);
        return fetch(`/api${path}`, { ...options, headers });
    };
    let response = await send();
    if (response.status === 401) {
        try {
            token = await accessToken(true);
            response = await send();
        } catch {
            throw new ApiError(401, 'Your session ended. Sign in again.');
        }
    }
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
