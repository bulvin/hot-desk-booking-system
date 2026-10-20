import { useEffect, useRef, useState, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, ApiError } from '../api/client';
import { utcToday } from '../lib/dates';
import type { Location, Page } from '../api/types';

export function useToday() {
    const [today, setToday] = useState(utcToday());
    useEffect(() => {
        const timer = window.setInterval(() => setToday(utcToday()), 1000);
        return () => clearInterval(timer);
    }, []);
    return today;
}
export function ErrorMessage({ error }: { error: unknown }) {
    if (!error) return null;
    return (
        <div role="alert" className="alert alert-error my-4">
            <div>
                <p>
                    {error instanceof Error
                        ? error.message
                        : 'Something went wrong. Please try again.'}
                </p>
                {error instanceof ApiError && (
                    <ul>
                        {Object.entries(error.errors).map(
                            ([field, messages]) => (
                                <li key={field}>
                                    {field}: {messages.join(' ')}
                                </li>
                            ),
                        )}
                    </ul>
                )}
            </div>
        </div>
    );
}
export function Field({
    label,
    children,
    error,
}: {
    label: string;
    children: ReactNode;
    error?: string;
}) {
    return (
        <label className="grid gap-2 py-2">
            <span className="text-sm font-semibold py-1">{label}</span>
            {children}
            {error && <span className="text-error text-sm">{error}</span>}
        </label>
    );
}
export function fieldError(error: unknown, name: string) {
    if (!(error instanceof ApiError)) return undefined;
    return Object.entries(error.errors)
        .filter(([key]) => key.toLowerCase() === name.toLowerCase())
        .flatMap(([, value]) => value)
        .join(' ');
}
export function Pager({
    page,
    next,
    previous,
    onChange,
}: {
    page: number;
    next: boolean;
    previous: boolean;
    onChange: (page: number) => void;
}) {
    return (
        <nav aria-label="Pagination" className="flex items-center gap-4 py-4">
            <button
                className="btn btn-sm"
                disabled={!previous}
                onClick={() => onChange(page - 1)}
            >
                Previous
            </button>
            <span className="text-sm">Page {page}</span>
            <button
                className="btn btn-sm"
                disabled={!next}
                onClick={() => onChange(page + 1)}
            >
                Next
            </button>
        </nav>
    );
}
export function useLocations() {
    return useQuery({
        queryKey: ['locations', 'all'],
        queryFn: async ({ signal }) => {
            const locations: Location[] = [];
            let page = 1;
            while (true) {
                const result = await api<Page<Location>>(
                    `/locations?page=${page}&pageSize=30`,
                    { signal },
                );
                locations.push(...result.items);
                if (!result.hasNextPage) return locations;
                page++;
            }
        },
    });
}
export function OfficeSelect({
    value,
    onChange,
    label = 'Office',
}: {
    value: string;
    onChange: (value: string) => void;
    label?: string;
}) {
    const locations = useLocations();
    return (
        <div>
            <Field label={label}>
                <select
                    className="select w-full"
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    required
                >
                    <option value="">Choose an office</option>
                    {locations.data?.map((location) => (
                        <option key={location.id} value={location.id}>
                            {location.name} / {location.address.city}
                        </option>
                    ))}
                </select>
            </Field>
            {locations.isPending && <p role="status">Loading offices...</p>}
            <ErrorMessage error={locations.error} />
            {locations.isError && (
                <button
                    className="btn btn-sm"
                    onClick={() => void locations.refetch()}
                >
                    Retry offices
                </button>
            )}
            {locations.data?.length === 0 && (
                <p className="text-sm py-2">
                    No offices yet. An administrator can add the first office.
                </p>
            )}
        </div>
    );
}
export function Confirm({
    title,
    children,
    pending,
    disabled = false,
    onConfirm,
    onClose,
}: {
    title: string;
    children: ReactNode;
    pending: boolean;
    disabled?: boolean;
    onConfirm: () => void;
    onClose: () => void;
}) {
    const ref = useRef<HTMLDialogElement>(null);
    useEffect(() => {
        const dialog = ref.current;
        dialog?.showModal();
        return () => dialog?.close();
    }, []);
    return (
        <dialog
            ref={ref}
            className="modal"
            aria-labelledby="confirm-title"
            onCancel={(e) => {
                if (pending) e.preventDefault();
                else onClose();
            }}
        >
            <div className="modal-box">
                <h2 id="confirm-title" className="font-bold text-xl">
                    {title}
                </h2>
                <div className="py-4">{children}</div>
                <div className="modal-action">
                    <button
                        className="btn"
                        disabled={pending}
                        onClick={onClose}
                    >
                        Back
                    </button>
                    <button
                        className="btn"
                        disabled={pending || disabled}
                        onClick={onConfirm}
                    >
                        {pending ? 'Saving...' : 'Confirm'}
                    </button>
                </div>
            </div>
        </dialog>
    );
}
export function Loading() {
    return (
        <p role="status" className="py-8 flex items-center gap-3">
            <span className="loading loading-spinner loading-sm" />
            Loading...
        </p>
    );
}
