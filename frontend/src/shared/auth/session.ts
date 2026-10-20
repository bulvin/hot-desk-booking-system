import Keycloak from 'keycloak-js';
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: { retry: false, staleTime: 15000 },
        mutations: { retry: false },
    },
});
export const keycloak = new Keycloak({
    url: import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8090',
    realm: import.meta.env.VITE_KEYCLOAK_REALM || 'hotdesk',
    clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'hotdesk-frontend',
});
export const callback = () => `${window.location.origin}/auth/callback`;
export function expireSession() {
    keycloak.clearToken();
    queryClient.clear();
    window.dispatchEvent(new Event('hotdesk:session-ended'));
}
keycloak.onAuthLogout = () => {
    queryClient.clear();
    window.dispatchEvent(new Event('hotdesk:session-ended'));
};
export async function logout() {
    queryClient.clear();
    await keycloak.logout({ redirectUri: `${window.location.origin}/` });
}

keycloak.onTokenExpired = () => {
    void keycloak.updateToken(30).catch(expireSession);
};
