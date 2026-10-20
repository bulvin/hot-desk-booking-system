export const utcToday = (now = new Date()) => now.toISOString().slice(0, 10);
export function addDays(date: string, days: number) {
    const value = new Date(`${date}T00:00:00Z`);
    value.setUTCDate(value.getUTCDate() + days);
    return value.toISOString().slice(0, 10);
}
export function bookingError(
    start: string,
    end: string,
    today = utcToday(),
): string | null {
    if (!start || !end) return 'Choose both booking dates.';
    if (start < today || start > addDays(today, 7))
        return 'Start within the next seven days, using the UTC calendar.';
    if (end < start || end > addDays(start, 6))
        return 'Choose an inclusive range of one to seven days.';
    return null;
}
export const canChange = (
    r: { status: string; startDate: string },
    today = utcToday(),
) => r.status === 'Reserved' && r.startDate > today;
export const canCancel = (
    r: { status: string; startDate: string; endDate: string },
    admin: boolean,
    today = utcToday(),
) =>
    r.status === 'Reserved' &&
    (admin ? r.endDate >= today : r.startDate > today);
