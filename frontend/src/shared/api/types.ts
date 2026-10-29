export interface Profile {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
    emailConfirmed: boolean;
}
export interface Account {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    isActive: boolean;
    isAdministrator: boolean;
}
export interface Address {
    street: string;
    buildingNumber: string;
    city: string;
    postalCode: string;
}
export interface Location {
    id: string;
    name: string;
    address: Address;
}
export interface Desk {
    id: string;
    name: string;
    description: string | null;
    locationId: string;
    isAvailable: boolean;
}
export interface Reservation {
    id: string;
    startDate: string;
    endDate: string;
    status: 'Reserved' | 'Canceled' | 'Completed';
}
export interface MyReservation extends Reservation {
    deskId: string;
    deskName: string;
    locationId: string;
    locationName: string;
}
export interface DeskDetails extends Desk {
    reservation:
        | (Reservation & { user: { id: string; name: string | null } | null })
        | null;
}
export interface Page<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalItems: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}
