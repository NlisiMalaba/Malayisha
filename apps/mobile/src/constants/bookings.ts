/**
 * Booking status as JSON string names (JsonStringEnumConverter on the API).
 */
export const BookingStatusName = {
  Requested: 'Requested',
  Quoted: 'Quoted',
  Confirmed: 'Confirmed',
  InTransit: 'InTransit',
  Delivered: 'Delivered',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
} as const;

export type BookingStatusNameValue =
  (typeof BookingStatusName)[keyof typeof BookingStatusName];

export const DEFAULT_BOOKING_PAGE_SIZE = 20;

export function normalizeBookingStatus(
  status: string | number | null | undefined,
): BookingStatusNameValue | null {
  if (status === BookingStatusName.Requested || status === 1) {
    return BookingStatusName.Requested;
  }
  if (status === BookingStatusName.Quoted || status === 2) {
    return BookingStatusName.Quoted;
  }
  if (status === BookingStatusName.Confirmed || status === 3) {
    return BookingStatusName.Confirmed;
  }
  if (status === BookingStatusName.InTransit || status === 4) {
    return BookingStatusName.InTransit;
  }
  if (status === BookingStatusName.Delivered || status === 5) {
    return BookingStatusName.Delivered;
  }
  if (status === BookingStatusName.Completed || status === 6) {
    return BookingStatusName.Completed;
  }
  if (status === BookingStatusName.Cancelled || status === 7) {
    return BookingStatusName.Cancelled;
  }
  return null;
}

export function formatBookingStatus(status: string | number | null | undefined): string {
  const normalized = normalizeBookingStatus(status);
  switch (normalized) {
    case BookingStatusName.InTransit:
      return 'In transit';
    case null:
      return String(status ?? 'Unknown');
    default:
      return normalized;
  }
}

export type BookingAction =
  | 'quote'
  | 'confirm'
  | 'inTransit'
  | 'delivered'
  | 'complete'
  | 'cancel';

export function availableBookingActions(
  status: string | number | null | undefined,
  role: string | null | undefined,
): BookingAction[] {
  const normalized = normalizeBookingStatus(status);
  const isSender = role === 'Sender';
  const isTransporter = role === 'Transporter';
  const actions: BookingAction[] = [];

  if (!normalized) {
    return actions;
  }

  if (normalized === BookingStatusName.Requested) {
    if (isTransporter) {
      actions.push('quote');
    }
    if (isSender || isTransporter) {
      actions.push('cancel');
    }
  }

  if (normalized === BookingStatusName.Quoted) {
    if (isSender) {
      actions.push('confirm');
    }
    if (isSender || isTransporter) {
      actions.push('cancel');
    }
  }

  if (normalized === BookingStatusName.Confirmed) {
    if (isTransporter) {
      actions.push('inTransit');
    }
    if (isSender || isTransporter) {
      actions.push('cancel');
    }
  }

  if (normalized === BookingStatusName.InTransit && isTransporter) {
    actions.push('delivered');
  }

  if (normalized === BookingStatusName.Delivered && isSender) {
    actions.push('complete');
  }

  return actions;
}
