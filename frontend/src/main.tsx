import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { App } from './app/App';
import { callback, keycloak, queryClient } from './shared/auth/session';
import './app/styles.css';

const root = createRoot(document.getElementById('root')!);
async function start() {
    await keycloak.init({
        onLoad: 'login-required',
        pkceMethod: 'S256',
        redirectUri: callback(),
    });
    if (window.location.pathname === '/auth/callback')
        window.history.replaceState({}, '', '/desks');
    root.render(
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <App />
            </BrowserRouter>
        </QueryClientProvider>,
    );
}
void start().catch(() => {
    root.render(
        <main className="max-w-lg mx-auto p-8 mt-20">
            <h1 className="text-2xl font-bold">Unable to connect to sign-in</h1>
            <p className="my-4">Please try again in a moment.</p>
            <button className="btn" onClick={() => window.location.reload()}>
                Try again
            </button>
        </main>,
    );
});
