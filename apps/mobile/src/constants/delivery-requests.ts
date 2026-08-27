/**
 * Delivery request status as JSON string names (JsonStringEnumConverter on the API).
 * Generated OpenAPI types incorrectly mark this as number.
 */
export const DeliveryRequestStatusName = {
  Active: 'Active',
  Cancelled: 'Cancelled',
  ConvertedToBooking: 'ConvertedToBooking',
} as const;

export type DeliveryRequestStatusNameValue =
  (typeof DeliveryRequestStatusName)[keyof typeof DeliveryRequestStatusName];

export const DEFAULT_REQUEST_PAGE_SIZE = 20;

export function isEditableDeliveryRequestStatus(
  status: string | number | null | undefined,
): boolean {
  return status === DeliveryRequestStatusName.Active || status === 1;
}

export function formatDeliveryRequestStatus(
  status: string | number | null | undefined,
): string {
  if (status === DeliveryRequestStatusName.Active || status === 1) {
    return 'Active';
  }
  if (status === DeliveryRequestStatusName.Cancelled || status === 2) {
    return 'Cancelled';
  }
  if (status === DeliveryRequestStatusName.ConvertedToBooking || status === 3) {
    return 'Booked';
  }
  return String(status ?? 'Unknown');
}
