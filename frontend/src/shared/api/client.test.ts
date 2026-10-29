import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { api, ApiError } from './client';
import { expireSession, accessToken } from '../auth/session';

vi.mock('../auth/session', () => ({
    accessToken: vi.fn(),
    expireSession: vi.fn(),
}));
const fetchMock = vi.fn();
beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', fetchMock);
    vi.mocked(accessToken).mockResolvedValue('test-access-token');
});
afterEach(() => vi.unstubAllGlobals());
it('refreshes the token before sending an authenticated request', async () => {
    fetchMock.mockResolvedValue(
        new Response(JSON.stringify({ id: 'profile' }), { status: 200 }),
    );
    expect(await api('/profile')).toEqual({ id: 'profile' });
    expect(accessToken).toHaveBeenCalledWith();
    expect(fetchMock.mock.calls[0][1].headers.get('Authorization')).toBe(
        'Bearer test-access-token',
    );
});
it('ends the session without sending requests when refresh fails', async () => {
    vi.mocked(accessToken).mockRejectedValue(new Error('Expired'));
    await expect(api('/profile')).rejects.toMatchObject({ status: 401 });
    expect(fetchMock).not.toHaveBeenCalled();
});
it('ends the session after one refresh and one failed retry', async () => {
    fetchMock.mockImplementation(() =>
        Promise.resolve(new Response('{}', { status: 401 })),
    );
    await expect(api('/profile')).rejects.toBeInstanceOf(ApiError);
    expect(expireSession).toHaveBeenCalledOnce();
    expect(fetchMock).toHaveBeenCalledTimes(2);
});
it('preserves validation details and handles no-content success', async () => {
    fetchMock.mockResolvedValueOnce(
        new Response(
            JSON.stringify({
                title: 'Validation failed',
                errors: { Name: ['Required'] },
            }),
            { status: 400 },
        ),
    );
    await expect(api('/locations')).rejects.toMatchObject({
        status: 400,
        errors: { Name: ['Required'] },
    });
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 204 }));
    expect(
        await api('/reservations/id/cancel', { method: 'PUT' }),
    ).toBeUndefined();
});
