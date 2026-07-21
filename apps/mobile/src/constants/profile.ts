export const VerificationStatusName = {
  Pending: 'Pending',
  Approved: 'Approved',
  Rejected: 'Rejected',
} as const;

export type VerificationStatusNameValue =
  (typeof VerificationStatusName)[keyof typeof VerificationStatusName];

export const PROFILE_PHOTO_CONTENT_TYPES = ['image/jpeg', 'image/png', 'image/webp'] as const;

/** Corridor city options used as transporter routes served. */
export const PROFILE_ROUTE_OPTIONS = [
  'Johannesburg',
  'Pretoria',
  'Harare',
  'Bulawayo',
] as const;

export function normalizeVerificationStatus(
  status: string | number | null | undefined,
): VerificationStatusNameValue | null {
  if (status === VerificationStatusName.Pending || status === 1) {
    return VerificationStatusName.Pending;
  }
  if (status === VerificationStatusName.Approved || status === 2) {
    return VerificationStatusName.Approved;
  }
  if (status === VerificationStatusName.Rejected || status === 3) {
    return VerificationStatusName.Rejected;
  }
  return null;
}

export function formatVerificationStatus(status: string | number | null | undefined): string {
  return normalizeVerificationStatus(status) ?? String(status ?? 'Unknown');
}

export function canApplyForVerification(
  isVerified: boolean,
  status: string | number | null | undefined,
): boolean {
  if (isVerified) {
    return false;
  }
  const normalized = normalizeVerificationStatus(status);
  return normalized == null || normalized === VerificationStatusName.Rejected;
}
