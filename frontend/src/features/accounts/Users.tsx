import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, body } from '../../shared/api/client';
import type { Account, Page } from '../../shared/api/types';
import {
    Confirm,
    ErrorMessage,
    Field,
    Loading,
    Pager,
} from '../../shared/ui/components';

export function Users() {
    const client = useQueryClient();
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState('');
    const [status, setStatus] = useState('');
    const [error, setError] = useState<unknown>(null);
    const [pending, setPending] = useState(false);
    const [notice, setNotice] = useState('');
    const [selected, setSelected] = useState<Account | null>(null);
    const accounts = useQuery({
        queryKey: ['users', page, search, status],
        queryFn: ({ signal }) =>
            api<Page<Account>>(
                `/users?page=${page}&pageSize=20&search=${encodeURIComponent(search)}${status ? `&isActive=${status}` : ''}`,
                { signal },
            ),
    });
    async function create(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const form = event.currentTarget;
        const data = new FormData(form);
        setPending(true);
        setError(null);
        setNotice('');
        try {
            await api('/users', {
                method: 'POST',
                body: body(Object.fromEntries(data)),
            });
            form.reset();
            setNotice(
                'Employee created. Share the assigned password privately.',
            );
            await client.invalidateQueries({ queryKey: ['users'] });
        } catch (error) {
            setError(error);
        } finally {
            setPending(false);
        }
    }
    async function toggle() {
        if (!selected) return;
        setPending(true);
        setError(null);
        try {
            await api(`/users/${selected.id}/active`, {
                method: 'PUT',
                body: body({ isActive: !selected.isActive }),
            });
            setSelected(null);
            await client.invalidateQueries({ queryKey: ['users'] });
        } catch (error) {
            setError(error);
        } finally {
            setPending(false);
        }
    }
    return (
        <section>
            <h1 className="text-3xl font-bold">Employees</h1>
            <p className="text-base-content/70 mt-2 mb-6">
                Add people to your team and manage their access to HotDesk.
            </p>
            <ErrorMessage error={error} />
            <div className="grid items-start gap-6 xl:grid-cols-[minmax(18rem,22rem)_minmax(0,1fr)]">
                <form
                    onSubmit={(event) => void create(event)}
                    className="card card-border bg-base-100 p-5 sm:p-6"
                    aria-labelledby="create-employee-title"
                >
                    <h2
                        id="create-employee-title"
                        className="text-xl font-bold"
                    >
                        Create an employee
                    </h2>
                    <p className="mt-2 mb-4 text-sm text-base-content/60">
                        Set up an account so they can start booking desks.
                    </p>
                    <div className="grid gap-x-4 sm:grid-cols-2">
                        <Field label="First name">
                            <input
                                className="input w-full"
                                name="firstName"
                                required
                                maxLength={100}
                                autoComplete="given-name"
                            />
                        </Field>
                        <Field label="Last name">
                            <input
                                className="input w-full"
                                name="lastName"
                                required
                                maxLength={100}
                                autoComplete="family-name"
                            />
                        </Field>
                    </div>
                    <Field label="Email">
                        <input
                            className="input w-full"
                            name="email"
                            type="email"
                            required
                            maxLength={256}
                            autoComplete="off"
                            placeholder="name@company.com"
                        />
                    </Field>
                    <Field label="Assigned password">
                        <input
                            className="input w-full"
                            name="password"
                            type="password"
                            required
                            minLength={8}
                            maxLength={128}
                            autoComplete="new-password"
                            aria-describedby="employee-password-hint"
                        />
                    </Field>
                    <p
                        id="employee-password-hint"
                        className="mt-1 text-xs leading-relaxed text-base-content/60"
                    >
                        At least 8 characters with uppercase, lowercase, a
                        digit, and a symbol. Share the password privately.
                    </p>
                    <div className="mt-6 border-t border-base-300 pt-5">
                        <button className="btn w-full" disabled={pending}>
                            {pending && !selected
                                ? 'Creating employee...'
                                : 'Create employee'}
                        </button>
                        <p role="status" className="mt-3 text-sm text-success">
                            {notice}
                        </p>
                    </div>
                </form>
                <section
                    className="card card-border min-w-0 bg-base-100"
                    aria-labelledby="employee-directory-title"
                >
                    <div className="border-b border-base-300 p-5 sm:p-6">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                            <h2
                                id="employee-directory-title"
                                className="text-xl font-bold"
                            >
                                Team directory
                            </h2>
                            {accounts.data && (
                                <span className="badge badge-ghost">
                                    {accounts.data.totalItems}{' '}
                                    {accounts.data.totalItems === 1
                                        ? 'account'
                                        : 'accounts'}
                                </span>
                            )}
                        </div>
                        <p className="mt-2 text-sm text-base-content/60">
                            Find an account by name or email and check its
                            status.
                        </p>
                        <div className="mt-4 grid items-end gap-x-4 gap-y-1 sm:grid-cols-[minmax(0,1fr)_10rem]">
                            <Field label="Search employees">
                                <input
                                    className="input w-full"
                                    type="search"
                                    placeholder="Name or email address"
                                    value={search}
                                    onChange={(event) => {
                                        setSearch(event.target.value);
                                        setPage(1);
                                    }}
                                    maxLength={100}
                                />
                            </Field>
                            <Field label="Account status">
                                <select
                                    className="select w-full"
                                    value={status}
                                    onChange={(event) => {
                                        setStatus(event.target.value);
                                        setPage(1);
                                    }}
                                >
                                    <option value="">All statuses</option>
                                    <option value="true">Active</option>
                                    <option value="false">Inactive</option>
                                </select>
                            </Field>
                        </div>
                        {(search || status) && (
                            <button
                                className="btn btn-ghost btn-sm mt-2"
                                onClick={() => {
                                    setSearch('');
                                    setStatus('');
                                    setPage(1);
                                }}
                            >
                                Clear filters
                            </button>
                        )}
                    </div>
                    <div className="px-5 sm:px-6">
                        <ErrorMessage error={accounts.error} />
                    </div>
                    {accounts.isPending ? (
                        <div className="px-6 py-10">
                            <Loading />
                        </div>
                    ) : accounts.data && accounts.data.items.length === 0 ? (
                        <div className="px-6 py-16 text-center">
                            <h3 className="font-semibold">
                                No employees found
                            </h3>
                            <p className="mt-2 text-sm text-base-content/60">
                                {search || status
                                    ? 'Try a different search or clear the filters.'
                                    : 'Create an employee to get your team started.'}
                            </p>
                        </div>
                    ) : accounts.data ? (
                        <div className="overflow-x-auto">
                            <table className="table min-w-[34rem]">
                                <caption className="sr-only">
                                    Employee accounts, roles, and access status
                                </caption>
                                <thead className="bg-base-200/60">
                                    <tr>
                                        <th className="pl-5 sm:pl-6">
                                            Employee
                                        </th>
                                        <th>Role</th>
                                        <th>Status</th>
                                        <th className="pr-5 text-right sm:pr-6">
                                            Actions
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {accounts.data.items.map((account) => (
                                        <tr
                                            key={account.id}
                                            className="hover:bg-base-200/40"
                                        >
                                            <td className="py-4 pl-5 sm:pl-6">
                                                <div className="flex items-center gap-3">
                                                    <div
                                                        className="avatar avatar-placeholder shrink-0"
                                                        aria-hidden="true"
                                                    >
                                                        <div className="w-10 rounded-full bg-base-200 text-base-content/70">
                                                            <span className="text-sm font-semibold">
                                                                {`${account.firstName.charAt(0)}${account.lastName.charAt(0)}`.toUpperCase() ||
                                                                    account.email
                                                                        .charAt(
                                                                            0,
                                                                        )
                                                                        .toUpperCase()}
                                                            </span>
                                                        </div>
                                                    </div>
                                                    <div>
                                                        <div className="font-semibold">
                                                            {[
                                                                account.firstName,
                                                                account.lastName,
                                                            ]
                                                                .filter(Boolean)
                                                                .join(' ') ||
                                                                account.email}
                                                        </div>
                                                        <div className="mt-0.5 text-xs text-base-content/60">
                                                            {account.email}
                                                        </div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>
                                                <span className="badge badge-sm badge-outline whitespace-nowrap">
                                                    {account.isAdministrator
                                                        ? 'Administrator'
                                                        : 'Employee'}
                                                </span>
                                            </td>
                                            <td>
                                                <span
                                                    className={`badge badge-sm ${account.isActive ? 'badge-soft badge-success' : 'badge-ghost'}`}
                                                >
                                                    {account.isActive
                                                        ? 'Active'
                                                        : 'Inactive'}
                                                </span>
                                            </td>
                                            <td className="pr-5 text-right sm:pr-6">
                                                {!account.isAdministrator && (
                                                    <button
                                                        className="btn btn-sm btn-ghost"
                                                        disabled={pending}
                                                        onClick={() =>
                                                            setSelected(account)
                                                        }
                                                    >
                                                        {account.isActive
                                                            ? 'Deactivate'
                                                            : 'Reactivate'}
                                                    </button>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    ) : null}
                    {accounts.data && (
                        <div className="border-t border-base-300 px-5 sm:px-6">
                            <Pager
                                page={page}
                                next={accounts.data.hasNextPage}
                                previous={accounts.data.hasPreviousPage}
                                onChange={setPage}
                            />
                        </div>
                    )}
                </section>
            </div>
            {selected && (
                <Confirm
                    title={`${selected.isActive ? 'Deactivate' : 'Reactivate'} ${selected.email}?`}
                    pending={pending}
                    onClose={() => setSelected(null)}
                    onConfirm={() => void toggle()}
                >
                    {selected.isActive
                        ? 'Deactivation immediately signs this employee out.'
                        : 'This employee will be able to sign in and book desks again.'}
                </Confirm>
            )}
        </section>
    );
}
