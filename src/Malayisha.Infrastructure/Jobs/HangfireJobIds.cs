namespace Malayisha.Infrastructure.Jobs;

public static class HangfireJobIds
{
    public const string AutoCompleteExpiredBookings = "auto-complete-expired-bookings";
    public const string ExpireBoosts = "expire-boosts";
    public const string RetryFailedNotifications = "retry-failed-notifications";
}
