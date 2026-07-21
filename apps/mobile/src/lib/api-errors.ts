import { extractErrorCode, messageForAuthError } from '@/lib/auth-errors';

const API_ERROR_MESSAGES: Record<string, string> = {
  RequiredDateMustBeFuture: 'Required date must be a future date.',
  DeliveryRequestNotFound: 'This delivery request was not found.',
  NotDeliveryRequestOwner: 'You can only manage your own delivery requests.',
  AssociatedBookingBlocksUpdate: 'This request already has a booking and cannot be edited.',
  ActiveBookingsBlockCancel:
    'This request has an active booking and cannot be cancelled.',
  BookingNotFound: 'This booking was not found.',
  NotBookingParticipant: 'You are not a participant on this booking.',
  TripNotFound: 'This trip was not found.',
  TransporterProfileNotFound: 'Transporter profile was not found.',
  DeliveryRequestNotActive: 'This delivery request is not active.',
  DeliveryRequestNotOwnedBySender: 'You can only book with your own delivery request.',
  DeliveryRequestAlreadyBooked: 'This delivery request is already linked to a booking.',
  SelfBookingNotAllowed: 'You cannot book your own trip.',
  InvalidStateTransition: 'This action is not allowed for the current booking status.',
};

export function messageForApiError(error: unknown, fallback?: string): string {
  const code = extractErrorCode(error);
  if (code && API_ERROR_MESSAGES[code]) {
    return API_ERROR_MESSAGES[code];
  }

  return messageForAuthError(code, fallback);
}
