import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, expect, it, vi } from 'vitest';
import { api, ApiError } from '../shared/api/client';
import { DeskSearch } from '../features/desks/DeskSearch';
import { Navigation } from '../app/App';
import { Reservations } from '../features/reservations/Reservations';
import { Admin } from '../features/administration/Admin';
import { addDays, utcToday } from '../shared/lib/dates';
import type { ReactNode } from 'react';

vi.mock('../shared/api/client', async () => {
    const original = await vi.importActual<
        typeof import('../shared/api/client')
    >('../shared/api/client');
    return { ...original, api: vi.fn() };
});
vi.mock('../shared/auth/session', () => ({
    keycloak: { authenticated: true },
    callback: vi.fn(),
    logout: vi.fn(),
}));
const request = vi.mocked(api);
const paged = (items: unknown[]) => ({
    items,
    totalItems: items.length,
    page: 1,
    pageSize: 12,
    hasNextPage: false,
    hasPreviousPage: false,
});
const office = { id: 'office', name: 'Warsaw', address: { city: 'Warsaw' } };
const desk = {
    id: 'desk',
    name: 'Window seat',
    description: 'Natural light',
    locationId: 'office',
    isAvailable: true,
};
const reservation = {
    id: 'reservation',
    deskId: 'desk',
    deskName: 'Window seat',
    locationId: 'office',
    locationName: 'Warsaw',
    startDate: addDays(utcToday(), 1),
    endDate: addDays(utcToday(), 1),
    status: 'Reserved',
};
function mount(node: ReactNode) {
    const client = new QueryClient({
        defaultOptions: {
            queries: { retry: false },
            mutations: { retry: false },
        },
    });
    render(
        <QueryClientProvider client={client}>
            <MemoryRouter>{node}</MemoryRouter>
        </QueryClientProvider>,
    );
    return client;
}
beforeEach(() => {
    request.mockReset();
    HTMLDialogElement.prototype.showModal = function () {
        this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close = function () {
        this.removeAttribute('open');
    };
    request.mockImplementation(async (path) =>
        path.startsWith('/locations?')
            ? paged([office])
            : path.startsWith('/reservations/mine')
              ? paged([reservation])
              : paged([desk]),
    );
});
it('shows administrator navigation only for administrators', () => {
    const view = render(
        <MemoryRouter>
            <Navigation admin={false} />
        </MemoryRouter>,
    );
    expect(
        screen.queryByRole('link', { name: 'Administration' }),
    ).not.toBeInTheDocument();
    view.rerender(
        <MemoryRouter>
            <Navigation admin />
        </MemoryRouter>,
    );
    expect(
        screen.getByRole('link', { name: 'Administration' }),
    ).toBeInTheDocument();
});
it('books the selected desk with calendar strings and refreshes reservation queries', async () => {
    const client = mount(<DeskSearch />);
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await screen.findByRole('option', { name: 'Warsaw / Warsaw' });
    await userEvent.selectOptions(
        await screen.findByRole('combobox', { name: 'Office' }),
        'office',
    );
    await userEvent.click(
        await screen.findByRole('button', { name: 'Book desk' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    await screen.findByRole('status');
    await waitFor(() =>
        expect(request).toHaveBeenCalledWith('/reservations', {
            method: 'POST',
            body: JSON.stringify({
                deskId: 'desk',
                startDate: utcToday(),
                endDate: utcToday(),
            }),
        }),
    );
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['reservations'] });
});
it('shows API conflicts in the booking dialog without retrying the mutation', async () => {
    request.mockImplementation(async (path, options) => {
        if (options?.method === 'POST')
            throw new ApiError(409, 'This desk was just booked.');
        return path.startsWith('/locations?') ? paged([office]) : paged([desk]);
    });
    mount(<DeskSearch />);
    await screen.findByRole('option', { name: 'Warsaw / Warsaw' });
    await userEvent.selectOptions(
        await screen.findByRole('combobox', { name: 'Office' }),
        'office',
    );
    await userEvent.click(
        await screen.findByRole('button', { name: 'Book desk' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
        'This desk was just booked.',
    );
    expect(
        request.mock.calls.filter(([, options]) => options?.method === 'POST'),
    ).toHaveLength(1);
});
it('cancels personal reservations only after confirmation', async () => {
    mount(<Reservations admin={false} />);
    await userEvent.click(
        await screen.findByRole('button', { name: 'Cancel booking' }),
    );
    expect(
        request.mock.calls.some(([, options]) => options?.method === 'PUT'),
    ).toBe(false);
    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    await waitFor(() =>
        expect(request).toHaveBeenCalledWith(
            '/reservations/reservation/cancel',
            { method: 'PUT' },
        ),
    );
});
it('offers desk changes for upcoming reservations', async () => {
    mount(<Reservations admin={false} />);
    await userEvent.click(
        await screen.findByRole('button', { name: 'Change desk' }),
    );
    expect(
        screen.getByRole('heading', { name: 'Change your desk' }),
    ).toBeInTheDocument();
});
it('shows a loading state and a retryable network error', async () => {
    let reject: (error: Error) => void = () => {};
    request.mockImplementation(
        () =>
            new Promise((_, rejectPromise) => {
                reject = rejectPromise;
            }),
    );
    mount(<Reservations admin={false} />);
    expect(screen.getByRole('status')).toHaveTextContent('Loading');
    reject(new Error('Network unavailable'));
    expect(await screen.findByRole('alert')).toHaveTextContent(
        'Network unavailable',
    );
    expect(
        screen.getByRole('button', { name: 'Retry reservations' }),
    ).toBeInTheDocument();
});
it('maps validation errors next to the office field', async () => {
    request.mockImplementation(async (path, options) => {
        if (options?.method === 'POST')
            throw new ApiError(400, 'Validation failed', {
                Name: ['Use a different name.'],
            });
        return path.startsWith('/locations?') ? paged([office]) : paged([]);
    });
    mount(<Admin />);
    await userEvent.type(
        screen.getByRole('textbox', { name: 'Office name' }),
        'Office',
    );
    for (const label of ['Street', 'Building number', 'City', 'Postal code'])
        await userEvent.type(
            screen.getByRole('textbox', { name: label }),
            'Test',
        );
    await userEvent.click(screen.getByRole('button', { name: 'Save office' }));
    const name = screen.getByRole('textbox', { name: /Office name/ });
    await waitFor(() =>
        expect(name.closest('label')).toHaveTextContent(
            'Use a different name.',
        ),
    );
});

it('removes the bookability filter when the checkbox is unchecked', async () => {
    mount(<DeskSearch />);
    await screen.findByRole('option', { name: 'Warsaw / Warsaw' });
    await userEvent.selectOptions(
        screen.getByRole('combobox', { name: 'Office' }),
        'office',
    );
    await screen.findByRole('button', { name: 'Book desk' });
    expect(
        request.mock.calls.some(
            ([path]) =>
                path.includes('isBookable=true') &&
                path.includes('isAvailable=true'),
        ),
    ).toBe(true);
    await userEvent.click(
        screen.getByRole('checkbox', { name: 'Only bookable desks' }),
    );
    await waitFor(() =>
        expect(
            request.mock.calls.some(
                ([path]) =>
                    path.startsWith('/locations/office/desks?') &&
                    !path.includes('isBookable='),
            ),
        ).toBe(true),
    );
});
it('changes a reservation to a different desk and refreshes reservations', async () => {
    request.mockImplementation(async (path) =>
        path.startsWith('/locations?')
            ? paged([office])
            : paged([{ ...desk, id: 'new-desk' }]),
    );
    const onChanged = vi.fn();
    const client = mount(
        <DeskSearch
            changing={
                reservation as import('../shared/api/types').MyReservation
            }
            onChanged={onChanged}
        />,
    );
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await screen.findByRole('option', { name: 'Warsaw / Warsaw' });
    await userEvent.selectOptions(
        screen.getByRole('combobox', { name: 'Office' }),
        'office',
    );
    await userEvent.click(
        await screen.findByRole('button', { name: 'Choose desk' }),
    );
    await userEvent.click(screen.getByRole('button', { name: 'Confirm' }));
    await waitFor(() => expect(onChanged).toHaveBeenCalledOnce());
    expect(request).toHaveBeenCalledWith(
        '/reservations/reservation/change-desk',
        { method: 'PUT', body: JSON.stringify({ deskId: 'new-desk' }) },
    );
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['reservations'] });
});
