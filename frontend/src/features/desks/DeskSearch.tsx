import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, body } from '../../shared/api/client';
import { addDays, bookingError, canChange } from '../../shared/lib/dates';
import {
    Confirm,
    ErrorMessage,
    Field,
    Loading,
    OfficeSelect,
    Pager,
    useToday,
} from '../../shared/ui/components';
import type {
    Desk,
    DeskDetails,
    MyReservation,
    Page,
} from '../../shared/api/types';

export function DeskSearch({
    changing,
    onChanged,
}: {
    changing?: MyReservation;
    onChanged?: () => void;
}) {
    const today = useToday();
    const [office, setOffice] = useState('');
    const [start, setStart] = useState(today);
    const [end, setEnd] = useState(today);
    const [bookable, setBookable] = useState(true);
    const [enabled, setEnabled] = useState('');
    const availability = bookable ? 'true' : enabled;
    const [page, setPage] = useState(1);
    const [selected, setSelected] = useState<Desk | null>(null);
    const [detailsId, setDetailsId] = useState('');
    const client = useQueryClient();
    const startDate = changing?.startDate ?? start;
    const endDate = changing?.endDate ?? end;
    const validation = changing
        ? canChange(changing, today)
            ? null
            : 'This reservation can no longer change desks.'
        : bookingError(startDate, endDate, today);
    const desks = useQuery({
        queryKey: [
            'desks',
            office,
            startDate,
            endDate,
            bookable,
            enabled,
            page,
        ],
        enabled: !!office && !validation,
        queryFn: ({ signal }) =>
            api<Page<Desk>>(
                `/locations/${office}/desks?startDate=${startDate}&endDate=${endDate}${bookable ? '&isBookable=true' : ''}${availability ? `&isAvailable=${availability}` : ''}&page=${page}&pageSize=12`,
                { signal },
            ),
    });
    const details = useQuery({
        queryKey: ['desk-details', office, detailsId],
        enabled: !!office && !!detailsId,
        queryFn: ({ signal }) =>
            api<DeskDetails>(`/locations/${office}/desks/${detailsId}`, {
                signal,
            }),
    });
    const mutation = useMutation({
        mutationFn: (desk: Desk) =>
            changing
                ? api<void>(`/reservations/${changing.id}/change-desk`, {
                      method: 'PUT',
                      body: body({ deskId: desk.id }),
                  })
                : api('/reservations', {
                      method: 'POST',
                      body: body({ deskId: desk.id, startDate, endDate }),
                  }),
        onSuccess: async () => {
            setSelected(null);
            await Promise.all([
                client.invalidateQueries({ queryKey: ['desks'] }),
                client.invalidateQueries({ queryKey: ['reservations'] }),
                client.invalidateQueries({ queryKey: ['desk-details'] }),
            ]);
            onChanged?.();
        },
        onError: () => {
            void client.invalidateQueries({ queryKey: ['desks'] });
        },
    });
    return (
        <section>
            <div className="mb-7">
                <p className="text-sm uppercase tracking-widest text-base-content/70">
                    Make room for a good day
                </p>
                <h1 className="text-3xl font-bold mt-2">
                    {changing ? 'Change your desk' : 'Find your next desk'}
                </h1>
                <p className="mt-2 text-base-content/70">
                    Choose your office, pick your days, and settle in.
                </p>
            </div>
            <div className="card card-border bg-base-100 p-5 mb-6">
                <div className="grid gap-4 md:grid-cols-3">
                    <OfficeSelect
                        value={office}
                        onChange={(value) => {
                            setOffice(value);
                            setPage(1);
                            setDetailsId('');
                        }}
                    />
                    {changing ? (
                        <p className="self-center md:col-span-2">
                            {startDate} - {endDate} / UTC calendar
                        </p>
                    ) : (
                        <>
                            <Field label="Start date (UTC)">
                                <input
                                    className="input w-full"
                                    type="date"
                                    required
                                    min={today}
                                    max={addDays(today, 7)}
                                    value={start}
                                    onChange={(e) => {
                                        setStart(e.target.value);
                                        setPage(1);
                                    }}
                                />
                            </Field>
                            <Field label="End date (UTC)">
                                <input
                                    className="input w-full"
                                    type="date"
                                    required
                                    min={start}
                                    max={start ? addDays(start, 6) : undefined}
                                    value={end}
                                    onChange={(e) => {
                                        setEnd(e.target.value);
                                        setPage(1);
                                    }}
                                />
                            </Field>
                        </>
                    )}
                </div>
                <div className="flex flex-wrap items-center gap-5 mt-4">
                    <label className="flex items-center gap-2">
                        <input
                            className="checkbox checkbox-sm"
                            type="checkbox"
                            checked={bookable}
                            onChange={(e) => {
                                setBookable(e.target.checked);
                                setPage(1);
                            }}
                        />
                        Only bookable desks
                    </label>
                    <Field label="Desk availability">
                        <select
                            className="select select-sm"
                            value={availability}
                            disabled={bookable}
                            onChange={(e) => {
                                setEnabled(e.target.value);
                                setPage(1);
                            }}
                        >
                            <option value="">All desks</option>
                            <option value="true">Enabled</option>
                            <option value="false">Disabled</option>
                        </select>
                    </Field>
                </div>
                {validation && (
                    <p role="alert" className="text-error mt-3">
                        {validation}
                    </p>
                )}
                <p className="text-xs text-base-content/60 mt-4">
                    Bookings cover 1-7 inclusive days. Start today or within 7
                    days. All date limits use midnight UTC.
                </p>
            </div>
            {desks.isFetching && <Loading />}
            <ErrorMessage error={desks.error} />
            {desks.isError && (
                <button className="btn" onClick={() => void desks.refetch()}>
                    Retry desks
                </button>
            )}
            {mutation.isSuccess && (
                <p role="status" className="alert alert-success mb-4">
                    {changing
                        ? 'Desk changed.'
                        : 'Your desk is booked. Find it in My reservations.'}
                </p>
            )}
            {desks.data && (
                <>
                    <p className="text-sm mb-4">
                        {desks.data.totalItems} desks found
                    </p>
                    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                        {desks.data.items.map((desk) => (
                            <article
                                key={desk.id}
                                className="card card-border bg-base-100"
                            >
                                <div className="card-body">
                                    <div className="flex justify-between gap-3">
                                        <h2 className="card-title">
                                            {desk.name}
                                        </h2>
                                        <span
                                            className={`badge ${desk.isAvailable ? 'badge-success badge-soft' : 'badge-ghost'}`}
                                        >
                                            {desk.isAvailable
                                                ? 'Enabled'
                                                : 'Disabled'}
                                        </span>
                                    </div>
                                    <p className="text-base-content/70">
                                        {desk.description ||
                                            'A space for your next office day.'}
                                    </p>
                                    <div className="card-actions mt-4">
                                        <button
                                            className="btn btn-sm"
                                            disabled={
                                                !desk.isAvailable ||
                                                !!validation ||
                                                mutation.isPending ||
                                                desk.id === changing?.deskId
                                            }
                                            onClick={() => {
                                                mutation.reset();
                                                setSelected(desk);
                                            }}
                                        >
                                            {changing
                                                ? 'Choose desk'
                                                : 'Book desk'}
                                        </button>
                                        <button
                                            className="btn btn-sm btn-ghost"
                                            onClick={() =>
                                                setDetailsId(desk.id)
                                            }
                                        >
                                            Details
                                        </button>
                                    </div>
                                </div>
                            </article>
                        ))}
                    </div>
                    {desks.data.items.length === 0 && (
                        <div className="card card-border bg-base-100 p-8">
                            <h2 className="font-bold">
                                No desks match your search
                            </h2>
                            <p>Try another office, date range, or filter.</p>
                        </div>
                    )}
                    <Pager
                        page={page}
                        next={desks.data.hasNextPage}
                        previous={desks.data.hasPreviousPage}
                        onChange={setPage}
                    />
                </>
            )}
            {detailsId && (
                <div className="card card-border bg-base-100 p-5 mt-4">
                    <button
                        className="btn btn-sm self-end"
                        onClick={() => setDetailsId('')}
                    >
                        Close details
                    </button>
                    {details.isPending && <Loading />}
                    <ErrorMessage error={details.error} />
                    {details.data && (
                        <>
                            <h2 className="text-xl font-bold">
                                {details.data.name}
                            </h2>
                            <p>{details.data.description}</p>
                            <p className="mt-2">
                                {details.data.isAvailable
                                    ? 'Enabled'
                                    : 'Disabled'}
                            </p>
                            {details.data.reservation && (
                                <p>
                                    Reservation:{' '}
                                    {details.data.reservation.startDate} -{' '}
                                    {details.data.reservation.endDate} (
                                    {details.data.reservation.status})
                                    {details.data.reservation.user?.name &&
                                        ` / ${details.data.reservation.user.name}`}
                                </p>
                            )}
                            <p className="text-sm text-base-content/60 mt-2">
                                Desk details do not show a complete schedule.
                                Search by dates to check bookability.
                            </p>
                        </>
                    )}
                </div>
            )}
            {selected && (
                <Confirm
                    title={
                        changing
                            ? 'Confirm desk change'
                            : 'Confirm your booking'
                    }
                    pending={mutation.isPending}
                    disabled={!!validation}
                    onClose={() => setSelected(null)}
                    onConfirm={() => mutation.mutate(selected)}
                >
                    <p>
                        {selected.name} / {startDate} - {endDate} (UTC)
                    </p>
                    <ErrorMessage error={mutation.error} />
                    {validation && (
                        <p role="alert" className="text-error">
                            {validation}
                        </p>
                    )}
                </Confirm>
            )}
        </section>
    );
}
