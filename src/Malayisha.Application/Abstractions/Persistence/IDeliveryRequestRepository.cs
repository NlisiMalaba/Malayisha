using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public sealed record DeliveryRequestListPage(
    IReadOnlyList<DeliveryRequest> Items,
    int TotalCount);

public interface IDeliveryRequestRepository
{
    Task<DeliveryRequest?> FindByIdAsync(
        Guid deliveryRequestId,
        CancellationToken cancellationToken = default);

    Task AddAsync(DeliveryRequest deliveryRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the delivery request has any associated booking.
    /// </summary>
    Task<bool> HasAssociatedBookingAsync(
        Guid deliveryRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the delivery request has at least one booking in
    /// Confirmed, InTransit, or Delivered status.
    /// </summary>
    Task<bool> HasBlockingBookingsAsync(
        Guid deliveryRequestId,
        CancellationToken cancellationToken = default);

    Task<DeliveryRequestListPage> ListActiveAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
