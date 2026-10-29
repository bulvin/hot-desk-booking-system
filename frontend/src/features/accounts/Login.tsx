import { useState, type FormEvent } from 'react';
import { login } from '../../shared/auth/session';
import { ErrorMessage, Field } from '../../shared/ui/components';

export function Login({ onSuccess }: { onSuccess: () => void }) {
    const [error, setError] = useState<unknown>(null);
    const [pending, setPending] = useState(false);
    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const form = event.currentTarget;
        const data = new FormData(form);
        setPending(true);
        setError(null);
        try {
            await login(
                String(data.get('email')),
                String(data.get('password')),
            );
            form.reset();
            onSuccess();
        } catch (error) {
            setError(error);
        } finally {
            setPending(false);
        }
    }
    return (
        <main className="max-w-lg mx-auto p-8 mt-20">
            <h1 className="text-3xl font-bold">Welcome to HotDesk</h1>
            <p className="my-5">Sign in to plan your next office day.</p>
            <form onSubmit={(event) => void submit(event)}>
                <Field label="Email">
                    <input
                        className="input w-full"
                        name="email"
                        type="email"
                        autoComplete="username"
                        required
                        maxLength={256}
                    />
                </Field>
                <Field label="Password">
                    <input
                        className="input w-full"
                        name="password"
                        type="password"
                        autoComplete="current-password"
                        required
                        maxLength={1024}
                    />
                </Field>
                <ErrorMessage error={error} />
                <button className="btn mt-4" disabled={pending}>
                    {pending ? 'Signing in?' : 'Sign in'}
                </button>
            </form>
            <p className="mt-6 text-sm">
                Your administrator provides your account and password.
            </p>
        </main>
    );
}
