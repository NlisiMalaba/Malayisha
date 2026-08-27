/**
 * Auth enums as JSON string names.
 * The API uses JsonStringEnumConverter; generated OpenAPI types incorrectly mark these as number.
 */
export const AuthRole = {
  Sender: 'Sender',
  Transporter: 'Transporter',
  Admin: 'Admin',
} as const;

export type AuthRoleValue = (typeof AuthRole)[keyof typeof AuthRole];

export const AuthOtpPurpose = {
  Register: 'Register',
  Login: 'Login',
} as const;

export type AuthOtpPurposeValue = (typeof AuthOtpPurpose)[keyof typeof AuthOtpPurpose];

export const AuthMode = {
  Register: 'register',
  Login: 'login',
} as const;

export type AuthModeValue = (typeof AuthMode)[keyof typeof AuthMode];

/** E.164 — mirrors AuthValidation.PhoneNumberPattern on the API. */
export const PHONE_E164_PATTERN = /^\+[1-9]\d{1,14}$/;

/** Six-digit OTP — mirrors VerifyOtp validation on the API. */
export const OTP_CODE_PATTERN = /^\d{6}$/;
