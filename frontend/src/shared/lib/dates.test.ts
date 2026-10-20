import { describe, expect, it } from 'vitest';
import { addDays, bookingError, canCancel, canChange, utcToday } from './dates';

describe('UTC booking rules', () => {
    it('uses UTC rather than the local date and crosses month boundaries', () => {
        expect(utcToday(new Date('2026-10-20T01:00:00+02:00'))).toBe(
            '2026-10-19',
        );
        expect(addDays('2026-12-31', 1)).toBe('2027-01-01');
    });
    it('allows inclusive one and seven day bookings and the advance boundary', () => {
        expect(
            bookingError('2026-10-19', '2026-10-19', '2026-10-19'),
        ).toBeNull();
        expect(
            bookingError('2026-10-26', '2026-11-01', '2026-10-19'),
        ).toBeNull();
    });
    it.each([
        ['2026-10-18', '2026-10-19'],
        ['2026-10-27', '2026-10-27'],
        ['2026-10-19', '2026-10-26'],
        ['2026-10-20', '2026-10-19'],
        ['', ''],
    ])('rejects invalid range %s to %s', (start, end) => {
        expect(bookingError(start, end, '2026-10-19')).not.toBeNull();
    });
    it('closes owner actions at UTC midnight but allows admin cancellation through the end', () => {
        const r = {
            status: 'Reserved',
            startDate: '2026-10-20',
            endDate: '2026-10-22',
        };
        expect(canChange(r, '2026-10-19')).toBe(true);
        expect(canChange(r, '2026-10-20')).toBe(false);
        expect(canCancel(r, false, '2026-10-20')).toBe(false);
        expect(canCancel(r, true, '2026-10-22')).toBe(true);
        expect(canCancel(r, true, '2026-10-23')).toBe(false);
    });
    it.each(['Canceled', 'Completed'])(
        'disallows changes to %s reservations',
        (status) => {
            const r = {
                status,
                startDate: '2026-10-20',
                endDate: '2026-10-22',
            };
            expect(canChange(r, '2026-10-19')).toBe(false);
            expect(canCancel(r, true, '2026-10-19')).toBe(false);
        },
    );
});
