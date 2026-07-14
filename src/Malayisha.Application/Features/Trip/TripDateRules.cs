namespace Malayisha.Application.Features.Trip;

internal static class TripDateRules
{
    /// <summary>
    /// Departure must be strictly after the current UTC calendar date (today and past are rejected).
    /// </summary>
    public static bool IsFutureDepartureDate(DateTime departureDateUtc, DateTime nowUtc) =>
        departureDateUtc.Date > nowUtc.Date;
}
