using System.Text.Json;
using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Trip.ApplyBoost;

public sealed record ApplyBoostCommand(
    Guid TripListingId,
    Guid AdminUserId,
    DateTime BoostStartAtUtc,
    DateTime BoostEndAtUtc) : IRequest<Result<BoostedTripDto>>;

internal sealed class ApplyBoostCommandValidator : AbstractValidator<ApplyBoostCommand>
{
    public ApplyBoostCommandValidator()
    {
        RuleFor(command => command.TripListingId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command)
            .Must(command => command.BoostEndAtUtc > command.BoostStartAtUtc)
            .WithErrorCode(TripErrorCodes.InvalidBoostWindow);
    }
}

internal sealed class ApplyBoostCommandHandler(
    ITripListingRepository tripListingRepository,
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider,
    ILogger<ApplyBoostCommandHandler> logger)
    : IRequestHandler<ApplyBoostCommand, Result<BoostedTripDto>>
{
    public async Task<Result<BoostedTripDto>> Handle(
        ApplyBoostCommand request,
        CancellationToken cancellationToken)
    {
        var trip = await tripListingRepository.FindByIdForUpdateAsync(
            request.TripListingId,
            cancellationToken);

        if (trip is null || trip.IsDeleted)
        {
            return Result<BoostedTripDto>.Error(TripErrorCodes.TripNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            trip.ApplyBoost(request.BoostStartAtUtc, request.BoostEndAtUtc, nowUtc);
        }
        catch (ArgumentException)
        {
            return Result<BoostedTripDto>.Error(TripErrorCodes.InvalidBoostWindow);
        }

        await auditLogRepository.AddAsync(
            AuditLog.Create(
                Guid.NewGuid(),
                request.AdminUserId,
                TripBoostAuditActions.Applied,
                TripBoostAuditActions.TargetType,
                trip.Id,
                nowUtc,
                JsonSerializer.Serialize(new
                {
                    boostStartAtUtc = request.BoostStartAtUtc,
                    boostEndAtUtc = request.BoostEndAtUtc
                })),
            cancellationToken);

        await tripListingRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Boost applied to trip listing {TripListingId} by admin {AdminUserId} until {BoostEndAtUtc}",
            trip.Id,
            request.AdminUserId,
            request.BoostEndAtUtc);

        return Result<BoostedTripDto>.Success(TripMappings.ToBoostedDto(trip));
    }
}
