import { afterEach, beforeEach, expect, it, vi } from 'vitest';

const fetchMock = vi.fn();
beforeEach(() => {
    vi.resetModules();
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
});
afterEach(() => vi.unstubAllGlobals());
const response = () =>
    new Response(
        JSON.stringify({
            accessToken: 'access',
            expiresAt: new Date(Date.now() + 600000).toISOString(),
        }),
        { status: 200 },
    );
it('restores a session and shares a refresh between concurrent requests', async () => {
    const session = await import('./session');
    fetchMock.mockImplementation(async () => response());
    const tokens = await Promise.all([
        session.accessToken(),
        session.accessToken(),
        session.accessToken(),
    ]);
    expect(tokens).toEqual(['access', 'access', 'access']);
    expect(fetchMock).toHaveBeenCalledOnce();
    expect(session.isAuthenticated()).toBe(true);
    expect(localStorage.length).toBe(0);
});
it('clears tokens and cached private data when refresh fails', async () => {
    const session = await import('./session');
    session.queryClient.setQueryData(['profile'], {
        email: 'private@example.test',
    });
    fetchMock.mockResolvedValue(new Response(null, { status: 401 }));
    await session.restoreSession();
    expect(session.isAuthenticated()).toBe(false);
    expect(session.queryClient.getQueryData(['profile'])).toBeUndefined();
});
it('revokes the server session before clearing local state on logout', async () => {
    const session = await import('./session');
    fetchMock
        .mockResolvedValueOnce(response())
        .mockResolvedValueOnce(new Response(null, { status: 204 }));
    await session.login('employee@example.test', 'assigned-password');
    await session.logout();
    expect(fetchMock.mock.calls[1][0]).toBe('/api/auth/logout');
    expect(session.isAuthenticated()).toBe(false);
});
