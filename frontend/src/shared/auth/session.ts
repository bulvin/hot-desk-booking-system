import { apiUrl } from '../api/url';
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: { retry: false, staleTime: 15000 },
        mutations: { retry: false },
    },
});
let token: string | null = null;
let expiresAt = 0;
let generation = 0;
let refreshing: Promise<string> | null = null;
export const isAuthenticated = () => token !== null;
export function expireSession() {
    generation++;
    token = null;
    expiresAt = 0;
    queryClient.clear();
    window.dispatchEvent(new Event('hotdesk:session-ended'));
}
async function accept(
    response: Response,
    expectedGeneration: number,
): Promise<string> {
    if (!response.ok)
        throw new Error(
            'Unable to sign in. Check your credentials or try again later.',
        );
    const data = (await response.json()) as {
        accessToken: string;
        expiresAt: string;
    };
    if (generation !== expectedGeneration) throw new Error('Session ended.');
    token = data.accessToken;
    expiresAt = Date.parse(data.expiresAt);
    return token;
}
export async function login(email: string, password: string) {
    const current = ++generation;
    await accept(
        await fetch(apiUrl('/auth/login'), {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
        }),
        current,
    );
    queryClient.clear();
}
export async function accessToken(force = false): Promise<string> {
    if (!force && token && expiresAt > Date.now() + 30000) return token;
    if (!refreshing) {
        const current = generation;
        // The Web Lock also serializes refreshes from different tabs sharing the cookie.
        const refresh = () =>
            fetch(apiUrl('/auth/refresh'), {
                method: 'POST',
                credentials: 'include',
            }).then((response) => accept(response, current));
        refreshing = Promise.resolve(
            navigator.locks
                ? navigator.locks.request('hotdesk-refresh', refresh)
                : refresh(),
        )
            .catch((error) => {
                if (current === generation) expireSession();
                throw error;
            })
            .finally(() => {
                refreshing = null;
            });
    }
    return refreshing!;
}
export async function restoreSession() {
    try {
        await accessToken();
    } catch {
        /* An absent session displays the login form. */
    }
}
export async function logout() {
    // Let any in-flight refresh settle before revoking the current cookie.
    await refreshing?.catch(() => undefined);
    const response = await fetch(apiUrl('/auth/logout'), {
        method: 'POST',
        credentials: 'include',
    });
    if (!response.ok) throw new Error('Sign out failed. Please try again.');
    expireSession();
}
