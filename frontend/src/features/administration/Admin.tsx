import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, body } from '../../shared/api/client';
import {
    Confirm,
    ErrorMessage,
    Field,
    fieldError,
    Loading,
    OfficeSelect,
    Pager,
    useLocations,
} from '../../shared/ui/components';
import type { Address, Desk, Location, Page } from '../../shared/api/types';

const emptyAddress: Address = {
    street: '',
    buildingNumber: '',
    city: '',
    postalCode: '',
};
export function Admin() {
    const client = useQueryClient();
    const offices = useLocations();
    const [editing, setEditing] = useState<Location | null>(null);
    const [name, setName] = useState('');
    const [address, setAddress] = useState<Address>(emptyAddress);
    const [office, setOffice] = useState('');
    const [deskName, setDeskName] = useState('');
    const [description, setDescription] = useState('');
    const [page, setPage] = useState(1);
    const [action, setAction] = useState<{
        title: string;
        path: string;
        method: string;
    } | null>(null);
    const invalidate = async () => {
        await Promise.all([
            client.invalidateQueries({ queryKey: ['locations'] }),
            client.invalidateQueries({ queryKey: ['admin-desks'] }),
            client.invalidateQueries({ queryKey: ['desks'] }),
            client.invalidateQueries({ queryKey: ['desk-details'] }),
        ]);
    };
    const saveOffice = useMutation({
        mutationFn: () =>
            api(`/locations${editing ? `/${editing.id}` : ''}`, {
                method: editing ? 'PUT' : 'POST',
                body: body({ name, address }),
            }),
        onSuccess: async () => {
            setEditing(null);
            setName('');
            setAddress(emptyAddress);
            await invalidate();
        },
    });
    const saveDesk = useMutation({
        mutationFn: () =>
            api(`/locations/${office}/desks`, {
                method: 'POST',
                body: body({
                    name: deskName,
                    description: description || null,
                }),
            }),
        onSuccess: async () => {
            setDeskName('');
            setDescription('');
            await invalidate();
        },
    });
    const mutate = useMutation({
        mutationFn: (request: NonNullable<typeof action>) =>
            api<void>(request.path, { method: request.method }),
        onSuccess: async () => {
            setAction(null);
            await invalidate();
        },
    });
    const desks = useQuery({
        queryKey: ['admin-desks', office, page],
        enabled: !!office,
        queryFn: ({ signal }) =>
            api<Page<Desk>>(
                `/locations/${office}/desks?page=${page}&pageSize=12`,
                { signal },
            ),
    });
    function edit(location: Location) {
        saveOffice.reset();
        setEditing(location);
        setName(location.name);
        setAddress(location.address);
        document.getElementById('office-name')?.focus();
    }
    function submitOffice(event: FormEvent) {
        event.preventDefault();
        saveOffice.mutate();
    }
    return (
        <section>
            <h1 className="text-3xl font-bold">Office administration</h1>
            <p className="text-base-content/70 mt-2 mb-6">
                Keep your offices and desks ready for the team.
            </p>
            <div className="grid gap-6 xl:grid-cols-2">
                <form
                    className="card card-border bg-base-100 p-5"
                    onSubmit={submitOffice}
                >
                    <h2 className="text-xl font-bold mb-3">
                        {editing ? 'Edit office' : 'Create an office'}
                    </h2>
                    <Field
                        label="Office name"
                        error={fieldError(saveOffice.error, 'Name')}
                    >
                        <input
                            id="office-name"
                            className="input w-full"
                            required
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                        />
                    </Field>
                    {(Object.keys(emptyAddress) as (keyof Address)[]).map(
                        (key) => (
                            <Field
                                key={key}
                                label={
                                    {
                                        street: 'Street',
                                        buildingNumber: 'Building number',
                                        city: 'City',
                                        postalCode: 'Postal code',
                                    }[key]
                                }
                                error={fieldError(
                                    saveOffice.error,
                                    `Address.${key}`,
                                )}
                            >
                                <input
                                    className="input w-full"
                                    required
                                    value={address[key]}
                                    onChange={(e) =>
                                        setAddress({
                                            ...address,
                                            [key]: e.target.value,
                                        })
                                    }
                                />
                            </Field>
                        ),
                    )}
                    <ErrorMessage error={saveOffice.error} />
                    {saveOffice.isSuccess && (
                        <p role="status" className="text-success mt-3">
                            Office saved.
                        </p>
                    )}
                    <div className="flex gap-2 mt-4">
                        <button className="btn" disabled={saveOffice.isPending}>
                            {saveOffice.isPending ? 'Saving...' : 'Save office'}
                        </button>
                        {editing && (
                            <button
                                type="button"
                                className="btn"
                                onClick={() => {
                                    setEditing(null);
                                    setName('');
                                    setAddress(emptyAddress);
                                    saveOffice.reset();
                                }}
                            >
                                Cancel edit
                            </button>
                        )}
                    </div>
                </form>
                <div>
                    <h2 className="text-xl font-bold mb-3">Offices</h2>
                    {offices.isPending && <Loading />}
                    <ErrorMessage error={offices.error} />
                    {offices.isError && (
                        <button
                            className="btn"
                            onClick={() => void offices.refetch()}
                        >
                            Retry offices
                        </button>
                    )}
                    <div className="grid gap-3">
                        {offices.data?.map((location) => (
                            <article
                                className="card card-border bg-base-100 p-4"
                                key={location.id}
                            >
                                <h3 className="font-bold">{location.name}</h3>
                                <p className="text-sm text-base-content/70">
                                    {location.address.street}{' '}
                                    {location.address.buildingNumber},{' '}
                                    {location.address.postalCode}{' '}
                                    {location.address.city}
                                </p>
                                <div className="flex gap-2 mt-3">
                                    <button
                                        className="btn btn-sm"
                                        onClick={() => edit(location)}
                                    >
                                        Edit
                                    </button>
                                    <button
                                        className="btn btn-sm btn-outline"
                                        onClick={() => {
                                            mutate.reset();
                                            setAction({
                                                title: `Delete office "${location.name}"?`,
                                                path: `/locations/${location.id}`,
                                                method: 'DELETE',
                                            });
                                        }}
                                    >
                                        Delete
                                    </button>
                                </div>
                            </article>
                        ))}
                    </div>
                    {offices.data?.length === 0 && <p>No offices yet.</p>}
                </div>
            </div>
            <div className="mt-10">
                <h2 className="text-2xl font-bold mb-4">Manage desks</h2>
                <OfficeSelect
                    value={office}
                    onChange={(value) => {
                        setOffice(value);
                        setPage(1);
                        saveDesk.reset();
                    }}
                />
                {office && (
                    <>
                        <form
                            className="card card-border bg-base-100 p-5 mt-4"
                            onSubmit={(e) => {
                                e.preventDefault();
                                saveDesk.mutate();
                            }}
                        >
                            <h3 className="font-bold mb-2">Add a desk</h3>
                            <div className="grid gap-4 md:grid-cols-2">
                                <Field
                                    label="Desk name"
                                    error={fieldError(saveDesk.error, 'Name')}
                                >
                                    <input
                                        className="input w-full"
                                        required
                                        value={deskName}
                                        onChange={(e) =>
                                            setDeskName(e.target.value)
                                        }
                                    />
                                </Field>
                                <Field
                                    label="Description"
                                    error={fieldError(
                                        saveDesk.error,
                                        'Description',
                                    )}
                                >
                                    <input
                                        className="input w-full"
                                        value={description}
                                        onChange={(e) =>
                                            setDescription(e.target.value)
                                        }
                                    />
                                </Field>
                            </div>
                            <ErrorMessage error={saveDesk.error} />
                            {saveDesk.isSuccess && (
                                <p role="status" className="text-success mt-3">
                                    Desk created.
                                </p>
                            )}
                            <button
                                className="btn mt-4 self-start"
                                disabled={saveDesk.isPending}
                            >
                                {saveDesk.isPending
                                    ? 'Saving...'
                                    : 'Create desk'}
                            </button>
                        </form>
                        {desks.isPending && <Loading />}
                        <ErrorMessage error={desks.error} />
                        {desks.isError && (
                            <button
                                className="btn"
                                onClick={() => void desks.refetch()}
                            >
                                Retry desks
                            </button>
                        )}
                        <div className="grid gap-3 mt-4">
                            {desks.data?.items.map((desk) => (
                                <article
                                    className="card card-border bg-base-100 p-4 flex-row flex-wrap justify-between gap-4"
                                    key={desk.id}
                                >
                                    <div>
                                        <h3 className="font-bold">
                                            {desk.name}
                                        </h3>
                                        <p>
                                            {desk.isAvailable
                                                ? 'Enabled'
                                                : 'Disabled'}
                                        </p>
                                    </div>
                                    <div className="flex flex-wrap gap-2">
                                        <button
                                            className="btn btn-sm"
                                            onClick={() => {
                                                mutate.reset();
                                                setAction({
                                                    title: `${desk.isAvailable ? 'Disable' : 'Enable'} "${desk.name}"?`,
                                                    path: `/locations/${office}/desks/${desk.id}?isAvailable=${!desk.isAvailable}`,
                                                    method: 'PUT',
                                                });
                                            }}
                                        >
                                            {desk.isAvailable
                                                ? 'Disable'
                                                : 'Enable'}
                                        </button>
                                        <button
                                            className="btn btn-sm btn-outline"
                                            onClick={() => {
                                                mutate.reset();
                                                setAction({
                                                    title: `Delete desk "${desk.name}"?`,
                                                    path: `/locations/${office}/desks/${desk.id}`,
                                                    method: 'DELETE',
                                                });
                                            }}
                                        >
                                            Delete
                                        </button>
                                    </div>
                                </article>
                            ))}
                        </div>
                        {desks.data?.items.length === 0 && (
                            <p className="py-4">No desks in this office yet.</p>
                        )}
                        {desks.data && (
                            <Pager
                                page={page}
                                next={desks.data.hasNextPage}
                                previous={desks.data.hasPreviousPage}
                                onChange={setPage}
                            />
                        )}
                    </>
                )}
            </div>
            {action && (
                <Confirm
                    title={action.title}
                    pending={mutate.isPending}
                    onClose={() => setAction(null)}
                    onConfirm={() => mutate.mutate(action)}
                >
                    <p>
                        Confirm this change. Desks with reservation history
                        cannot be deleted.
                    </p>
                    <ErrorMessage error={mutate.error} />
                </Confirm>
            )}
        </section>
    );
}
