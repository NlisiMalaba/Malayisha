using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Trip.RemoveBoost;

public sealed record RemoveBoostCommand(Guid TripListingId, Guid AdminUserId)
    : IRequest<Result<BoostedTripDto>>;

internal sealed class RemoveBoostCommandValidator : AbstractValidator<RemoveBoostCommand>
{
    public RemoveBoostCommandValidator()
    {
        RuleFor(command => command.TripListingId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
    }
}

internal sealed class RemoveBoostCommandHandler(
    ITripListingRepository tripListingRepository,
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider,
    ILogger<RemoveBoostCommandHandler> logger)
    : IRequestHandler<RemoveBoostCommand, Result<BoostedTripDto>>
{
    public async Task<Result<BoostedTripDto>> Handle(
        RemoveBoostCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await tripListingRepository.FindByIdForUpdateAsync(
            request.TripListingId,
            cancellationToken);

        if (trip is null || trip.IsDeleted)
        {
            return Result<BoostedTripDto>.Error(TripErrorCodes.TripNotFound);
        }

        if (!trip.IsBoosted)
        {
            return Result<BoostedTripDto>.Error(TripErrorCodes.TripNotBoosted);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        trip.ClearBoost(nowUtc);

        await auditLogRepository.AddAsync(
            AuditLog.Create(
                Guid.NewGuid(),
                request.AdminUserId,
                TripBoostAuditActions.Removed,
                TripBoostAuditActions.TargetType,
                trip.Id,
                nowUtc),
            cancellationToken);

        await tripListingRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Boost removed from trip listing {TripListingId} by admin {AdminUserId}",
            trip.Id,
            request.AdminUserId);

        return Result<BoostedTripDto>.Success(TripMappings.ToBoostedDto(trip));
    }
}
