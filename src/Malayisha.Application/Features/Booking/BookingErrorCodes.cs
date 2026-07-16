namespace Malayisha.Application.Features.Booking;

public static class BookingErrorCodes
{
    public const string BookingNotFound = "BookingNotFound";
    public const string TripNotFound = "TripNotFound";
    public const string TransporterProfileNotFound = "TransporterProfileNotFound";
    public const string DeliveryRequestNotFound = "DeliveryRequestNotFound";
    public const string DeliveryRequestNotActive = "DeliveryRequestNotActive";
    public const string DeliveryRequestNotOwnedBySender = "DeliveryRequestNotOwnedBySender";
    public const string DeliveryRequestAlreadyBooked = "DeliveryRequestAlreadyBooked";
    public const string SelfBookingNotAllowed = "SelfBookingNotAllowed";
}
