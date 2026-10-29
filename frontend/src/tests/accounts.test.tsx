import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, expect, it, vi } from 'vitest';
import { Login } from '../features/accounts/Login';
import { Users } from '../features/accounts/Users';
import { api } from '../shared/api/client';
import { login } from '../shared/auth/session';

vi.mock('../shared/auth/session', () => ({ login: vi.fn() }));
vi.mock('../shared/api/client', async () => ({
    ...(await vi.importActual<typeof import('../shared/api/client')>(
        '../shared/api/client',
    )),
    api: vi.fn(),
}));
beforeEach(() => {
    vi.clearAllMocks();
    HTMLDialogElement.prototype.showModal = function () {
        this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close = function () {
        this.removeAttribute('open');
    };
});
it('signs in with assigned credentials and clears the form', async () => {
    vi.mocked(login).mockResolvedValue();
    const done = vi.fn();
    render(<Login onSuccess={done} />);
    await userEvent.type(
        screen.getByLabelText('Email'),
        'employee@example.test',
    );
    await userEvent.type(
        screen.getByLabelText('Password'),
        'Assigned-password-123!',
    );
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    await waitFor(() => expect(done).toHaveBeenCalledOnce());
    expect(login).toHaveBeenCalledWith(
        'employee@example.test',
        'Assigned-password-123!',
    );
    expect(screen.getByLabelText('Password')).toHaveValue('');
});
it('shows login failure without navigating into the application', async () => {
    vi.mocked(login).mockRejectedValue(new Error('Invalid credentials'));
    const done = vi.fn();
    render(<Login onSuccess={done} />);
    await userEvent.type(
        screen.getByLabelText('Email'),
        'employee@example.test',
    );
    await userEvent.type(screen.getByLabelText('Password'), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
        'Invalid credentials',
    );
    expect(done).not.toHaveBeenCalled();
});
it('creates employees and confirms deactivation while protecting administrator controls', async () => {
    const accounts = [
        {
            id: 'employee',
            email: 'employee@example.test',
            firstName: 'Test',
            lastName: 'Employee',
            isActive: true,
            isAdministrator: false,
        },
        {
            id: 'admin',
            email: 'admin@example.test',
            firstName: 'Office',
            lastName: 'Admin',
            isActive: true,
            isAdministrator: true,
        },
    ];
    vi.mocked(api).mockImplementation(async (_path, options) =>
        options?.method
            ? undefined
            : {
                  items: accounts,
                  page: 1,
                  pageSize: 20,
                  totalItems: 2,
                  hasNextPage: false,
                  hasPreviousPage: false,
              },
    );
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false } },
    });
    render(
        <QueryClientProvider client={client}>
            <Users />
        </QueryClientProvider>,
    );
    await userEvent.type(screen.getByLabelText('First name'), 'New');
    await userEvent.type(screen.getByLabelText('Last name'), 'Employee');
    await userEvent.type(screen.getByLabelText('Email'), 'new@example.test');
    await userEvent.type(
        screen.getByLabelText('Assigned password'),
        'Assigned-password-123!',
    );
    await userEvent.click(
        screen.getByRole('button', { name: 'Create employee' }),
    );
    await waitFor(() =>
        expect(screen.getByRole('status')).toHaveTextContent(
            'Employee created',
        ),
    );
    expect(screen.getByLabelText('Assigned password')).toHaveValue('');
    expect(screen.getAllByRole('button', { name: 'Deactivate' })).toHaveLength(
        1,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Deactivate' }));
    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    await waitFor(() =>
        expect(api).toHaveBeenCalledWith('/users/employee/active', {
            method: 'PUT',
            body: JSON.stringify({ isActive: false }),
        }),
    );
});
