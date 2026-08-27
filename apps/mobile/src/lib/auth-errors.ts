const AUTH_ERROR_MESSAGES: Record<string, string> = {
  PhoneAlreadyRegistered: 'This phone number is already registered. Try logging in instead.',
  InvalidRole: 'Choose Sender or Transporter to continue.',
  UserNotFound: 'No account found for this phone number. Register to get started.',
  UserInactive: 'This account is inactive. Contact support for help.',
  PhoneLockedOut: 'Too many incorrect codes. Try again in 15 minutes.',
  OtpSendRateLimited: 'Too many OTP requests. Please wait a moment and try again.',
  InvalidOtp: 'Incorrect code. Check the SMS and try again.',
  OtpExpired: 'This code has expired. Request a new one.',
  InvalidRefreshToken: 'Your session expired. Please sign in again.',
  RefreshTokenExpired: 'Your session expired. Please sign in again.',
  RefreshTokenRevoked: 'Your session is no longer valid. Please sign in again.',
  RefreshTokenAlreadyUsed: 'Your session is no longer valid. Please sign in again.',
};

export function messageForAuthError(errorCode: string | undefined, fallback?: string): string {
  if (errorCode && AUTH_ERROR_MESSAGES[errorCode]) {
    return AUTH_ERROR_MESSAGES[errorCode];
  }

  return fallback ?? 'Something went wrong. Please try again.';
}

export function extractErrorCode(error: unknown): string | undefined {
  if (error && typeof error === 'object' && 'errorCode' in error) {
    const code = (error as { errorCode?: unknown }).errorCode;
    return typeof code === 'string' ? code : undefined;
  }

  return undefined;
}
