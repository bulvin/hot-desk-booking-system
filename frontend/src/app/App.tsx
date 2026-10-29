import { useEffect, useState } from 'react';
import { NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api } from '../shared/api/client';
import { isAuthenticated, logout } from '../shared/auth/session';
import { ErrorMessage, Loading } from '../shared/ui/components';
import { DeskSearch } from '../features/desks/DeskSearch';
import { Reservations } from '../features/reservations/Reservations';
import { Login } from '../features/accounts/Login';
import { Users } from '../features/accounts/Users';
import { Admin } from '../features/administration/Admin';
import type { Profile } from '../shared/api/types';

export function Navigation({ admin }: { admin: boolean }) {
    return (
        <nav aria-label="Main navigation" className="flex flex-wrap gap-2">
            {[
                ['/desks', 'Find a desk'],
                ['/reservations', 'My reservations'],
                ...(admin
                    ? [
                          ['/admin', 'Administration'],
                          ['/users', 'Employees'],
                      ]
                    : []),
            ].map(([to, label]) => (
                <NavLink
                    key={to}
                    to={to}
                    className={({ isActive }) =>
                        `btn btn-sm ${isActive ? 'btn-active' : 'btn-ghost'}`
                    }
                >
                    {label}
                </NavLink>
            ))}
        </nav>
    );
}
export function App() {
    const [ended, setEnded] = useState(!isAuthenticated());
    const [logoutError, setLogoutError] = useState<unknown>(null);
    useEffect(() => {
        const listener = () => setEnded(true);
        window.addEventListener('hotdesk:session-ended', listener);
        return () =>
            window.removeEventListener('hotdesk:session-ended', listener);
    }, []);
    const profile = useQuery({
        queryKey: ['profile'],
        queryFn: ({ signal }) => api<Profile>('/profile', { signal }),
        enabled: !ended,
    });
    const admin = profile.data?.roles.includes('Administrator') ?? false;
    if (ended) return <Login onSuccess={() => setEnded(false)} />;
    return (
        <div className="min-h-screen bg-base-200 text-base-content">
            <a href="#main" className="sr-only focus:not-sr-only">
                Skip to content
            </a>
            <header className="bg-base-100 border-b border-base-300">
                <div className="max-w-7xl mx-auto px-5 py-5 flex flex-wrap items-center justify-between gap-5">
                    <a
                        href="/desks"
                        className="flex items-center gap-3 font-bold text-2xl"
                    >
                        <span className="rounded-xl bg-neutral text-neutral-content px-3 py-2 text-lg">
                            H.
                        </span>
                        HotDesk
                    </a>
                    <Navigation admin={admin} />
                    <div className="flex items-center gap-3 text-sm">
                        <span>
                            {profile.data?.firstName} {profile.data?.lastName}
                        </span>
                        <button
                            className="btn btn-sm btn-ghost"
                            onClick={() => void logout().catch(setLogoutError)}
                        >
                            Sign out
                        </button>
                    </div>
                </div>
            </header>
            <main id="main" className="max-w-7xl mx-auto px-5 py-9">
                <ErrorMessage error={logoutError} />
                {profile.isPending ? (
                    <Loading />
                ) : profile.isError ? (
                    <>
                        <ErrorMessage error={profile.error} />
                        <button
                            className="btn"
                            onClick={() => void profile.refetch()}
                        >
                            Retry profile
                        </button>
                    </>
                ) : (
                    <Routes>
                        <Route
                            path="/users"
                            element={
                                admin ? (
                                    <Users />
                                ) : (
                                    <Navigate to="/desks" replace />
                                )
                            }
                        />
                        <Route path="/desks" element={<DeskSearch />} />
                        <Route
                            path="/reservations"
                            element={<Reservations admin={admin} />}
                        />
                        <Route
                            path="/admin"
                            element={
                                admin ? (
                                    <Admin />
                                ) : (
                                    <Navigate to="/desks" replace />
                                )
                            }
                        />
                        <Route
                            path="*"
                            element={<Navigate to="/desks" replace />}
                        />
                    </Routes>
                )}
            </main>
            <footer className="max-w-7xl mx-auto px-5 py-6 text-sm text-base-content/60 border-t border-base-300">
                A little less planning. A little more together.{' '}
                <span className="float-right">Booking calendar: UTC</span>
            </footer>
        </div>
    );
}
