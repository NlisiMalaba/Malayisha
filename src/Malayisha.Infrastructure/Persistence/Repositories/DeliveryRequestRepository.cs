using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class DeliveryRequestRepository(MalayishaDbContext dbContext) : IDeliveryRequestRepository
{
    private static readonly BookingStatus[] BlockingStatuses =
    [
        BookingStatus.Confirmed,
        BookingStatus.InTransit,
        BookingStatus.Delivered
    ];

    public Task<DeliveryRequest?> FindByIdAsync(
        Guid deliveryRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.DeliveryRequests
            .FirstOrDefaultAsync(request => request.Id == deliveryRequestId, cancellationToken);

    public async Task AddAsync(DeliveryRequest deliveryRequest, CancellationToken cancellationToken = default)
    {
        await dbContext.DeliveryRequests.AddAsync(deliveryRequest, cancellationToken);
    }

    public Task<bool> HasAssociatedBookingAsync(
        Guid deliveryRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(booking => booking.DeliveryRequestId == deliveryRequestId, cancellationToken);

    public Task<bool> HasBlockingBookingsAsync(
        Guid deliveryRequestId,
        CancellationToken cancellationToken = default) =>
        dbContext.Bookings
            .AsNoTracking()
            .AnyAsync(
                booking => booking.DeliveryRequestId == deliveryRequestId
                    && BlockingStatuses.Contains(booking.Status),
                cancellationToken);

    public async Task<DeliveryRequestListPage> ListActiveAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.DeliveryRequests
            .AsNoTracking()
            .Where(request => request.Status == DeliveryRequestStatus.Active);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(request => request.RequiredDateUtc)
            .ThenBy(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new DeliveryRequestListPage(items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
