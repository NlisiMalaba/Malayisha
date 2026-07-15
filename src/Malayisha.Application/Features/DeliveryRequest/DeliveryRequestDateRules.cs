namespace Malayisha.Application.Features.DeliveryRequest;

internal static class DeliveryRequestDateRules
{
    /// <summary>
    /// Required date must be strictly after the current UTC calendar date (today and past are rejected).
    /// </summary>
    public static bool IsFutureRequiredDate(DateTime requiredDateUtc, DateTime nowUtc) =>
        requiredDateUtc.Date > nowUtc.Date;
}
