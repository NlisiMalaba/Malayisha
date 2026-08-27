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
  ProfileAlreadyExists: 'You already have a transporter profile.',
  ProfileNotFound: 'Transporter profile was not found.',
  InvalidProfilePhoto: 'Use a JPEG, PNG, or WebP image for your profile photo.',
  ActiveVerificationExists: 'You already have a pending or approved verification application.',
  VerificationNotFound: 'Verification application was not found.',
  InvalidVerificationStatus: 'This verification cannot be updated in its current status.',
  CommissionRecordNotFound: 'Commission record was not found.',
  InvalidCommissionStatus: 'This commission status change is not allowed.',
  InvalidDateRange: 'Check the commission date range filters.',
  ReviewNotFound: 'Review was not found.',
  ReviewAlreadyHidden: 'This review is already hidden.',
  ReviewNotHidden: 'This review is not hidden.',
  UserNotFound: 'Your account was not found.',
  UserInactive: 'Your account is inactive.',
};

export function messageForApiError(error: unknown, fallback?: string): string {
  const code = extractErrorCode(error);
  if (code && API_ERROR_MESSAGES[code]) {
    return API_ERROR_MESSAGES[code];
  }

  return messageForAuthError(code, fallback);
}
