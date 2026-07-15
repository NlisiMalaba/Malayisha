namespace Malayisha.Application.Features.DeliveryRequest;

public static class DeliveryRequestErrorCodes
{
    public const string DeliveryRequestNotFound = "DeliveryRequestNotFound";
    public const string NotDeliveryRequestOwner = "NotDeliveryRequestOwner";
    public const string RequiredDateMustBeFuture = "RequiredDateMustBeFuture";
    public const string AssociatedBookingBlocksUpdate = "AssociatedBookingBlocksUpdate";
    public const string ActiveBookingsBlockCancel = "ActiveBookingsBlockCancel";
}
