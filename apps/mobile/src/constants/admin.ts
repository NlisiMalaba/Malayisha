export const CommissionStatusName = {
  Pending: 'Pending',
  Invoiced: 'Invoiced',
  Paid: 'Paid',
} as const;

export type CommissionStatusNameValue =
  (typeof CommissionStatusName)[keyof typeof CommissionStatusName];

export function normalizeCommissionStatus(
  status: string | number | null | undefined,
): CommissionStatusNameValue | null {
  if (status === CommissionStatusName.Pending || status === 1) {
    return CommissionStatusName.Pending;
  }
  if (status === CommissionStatusName.Invoiced || status === 2) {
    return CommissionStatusName.Invoiced;
  }
  if (status === CommissionStatusName.Paid || status === 3) {
    return CommissionStatusName.Paid;
  }
  return null;
}

export function formatCommissionStatus(status: string | number | null | undefined): string {
  return normalizeCommissionStatus(status) ?? String(status ?? 'Unknown');
}

/** API accepts string enum names via JsonStringEnumConverter. */
export function commissionStatusQueryValue(
  status: CommissionStatusNameValue | 'All',
): string | undefined {
  return status === 'All' ? undefined : status;
}
