import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../shared/api/client';
import { canCancel, canChange } from '../../shared/lib/dates';
import {
    Confirm,
    ErrorMessage,
    Loading,
    Pager,
    useToday,
} from '../../shared/ui/components';
import { DeskSearch } from '../desks/DeskSearch';
import type { MyReservation, Page } from '../../shared/api/types';

export function Reservations({ admin }: { admin: boolean }) {
    const [page, setPage] = useState(1);
    const [canceling, setCanceling] = useState<MyReservation | null>(null);
    const [changing, setChanging] = useState<MyReservation | null>(null);
    const today = useToday();
    const client = useQueryClient();
    const reservations = useQuery({
        queryKey: ['reservations', page],
        queryFn: ({ signal }) =>
            api<Page<MyReservation>>(
                `/reservations/mine?page=${page}&pageSize=12`,
                { signal },
            ),
    });
    const cancel = useMutation({
        mutationFn: (id: string) =>
            api<void>(`/reservations/${id}/cancel`, { method: 'PUT' }),
        onSuccess: async () => {
            setCanceling(null);
            await Promise.all([
                client.invalidateQueries({ queryKey: ['reservations'] }),
                client.invalidateQueries({ queryKey: ['desks'] }),
                client.invalidateQueries({ queryKey: ['desk-details'] }),
            ]);
        },
    });
    if (changing)
        return (
            <div>
                <button
                    className="btn btn-ghost mb-4"
                    onClick={() => setChanging(null)}
                >
                    Back to My reservations
                </button>
                <DeskSearch
                    changing={changing}
                    onChanged={() => setChanging(null)}
                />
            </div>
        );
    return (
        <section>
            <h1 className="text-3xl font-bold">My reservations</h1>
            <p className="text-base-content/70 mt-2 mb-6">
                Your upcoming office days and booking history.
            </p>
            <p className="text-sm mb-5">
                Change desks through the start day.{' '}
                {admin
                    ? 'As an administrator, you may cancel through the final booking day.'
                    : 'Cancel through the start day.'}{' '}
                Cutoffs use midnight UTC.
            </p>
            {reservations.isPending && <Loading />}
            <ErrorMessage error={reservations.error} />
            {reservations.isError && (
                <button
                    className="btn"
                    onClick={() => void reservations.refetch()}
                >
                    Retry reservations
                </button>
            )}
            {cancel.isSuccess && (
                <p role="status" className="alert alert-success mb-4">
                    Reservation canceled.
                </p>
            )}
            <div className="grid gap-4">
                {reservations.data?.items.map((r) => (
                    <article
                        className="card card-border bg-base-100 p-5"
                        key={r.id}
                    >
                        <div className="flex flex-wrap justify-between gap-4">
                            <div>
                                <h2 className="text-lg font-bold">
                                    {r.deskName}{' '}
                                    <span className="font-normal text-base-content/60">
                                        / {r.locationName}
                                    </span>
                                </h2>
                                <p className="mt-2">
                                    {r.startDate} - {r.endDate}{' '}
                                    <span className="text-xs">UTC</span>
                                </p>
                                <span className="badge badge-outline mt-3">
                                    {r.status === 'Reserved' &&
                                    r.endDate < today
                                        ? 'Completed'
                                        : r.status}
                                </span>
                            </div>
                            <div className="flex flex-wrap items-center gap-2">
                                <button
                                    className="btn btn-sm"
                                    disabled={!canChange(r, today)}
                                    onClick={() => setChanging(r)}
                                >
                                    Change desk
                                </button>
                                <button
                                    className="btn btn-sm btn-outline"
                                    disabled={!canCancel(r, admin, today)}
                                    onClick={() => {
                                        cancel.reset();
                                        setCanceling(r);
                                    }}
                                >
                                    Cancel booking
                                </button>
                            </div>
                        </div>
                    </article>
                ))}
            </div>
            {reservations.data?.items.length === 0 && (
                <div className="card card-border bg-base-100 p-8">
                    <h2 className="font-bold">No reservations on this page</h2>
                    <p>Find a desk to plan your next office day.</p>
                </div>
            )}
            {reservations.data && (
                <Pager
                    page={page}
                    next={reservations.data.hasNextPage}
                    previous={reservations.data.hasPreviousPage}
                    onChange={setPage}
                />
            )}
            {canceling && (
                <Confirm
                    title="Cancel this booking?"
                    pending={cancel.isPending}
                    disabled={!canCancel(canceling, admin, today)}
                    onClose={() => setCanceling(null)}
                    onConfirm={() => cancel.mutate(canceling.id)}
                >
                    <p>
                        {canceling.deskName} / {canceling.startDate} -{' '}
                        {canceling.endDate}
                    </p>
                    <p className="mt-2">
                        This frees the desk for the entire date range. The
                        booking stays in your history.
                    </p>
                    <ErrorMessage error={cancel.error} />
                </Confirm>
            )}
        </section>
    );
}
