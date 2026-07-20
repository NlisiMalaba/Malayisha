namespace Malayisha.Application.Features.Trip;

public static class TripErrorCodes
{
    public const string ProfileNotFound = "ProfileNotFound";
    public const string TripNotFound = "TripNotFound";
    public const string NotTripOwner = "NotTripOwner";
    public const string DepartureDateMustBeFuture = "DepartureDateMustBeFuture";
    public const string ActiveBookingsBlockDelete = "ActiveBookingsBlockDelete";
    public const string InvalidBoostWindow = "InvalidBoostWindow";
    public const string TripNotBoosted = "TripNotBoosted";
}
