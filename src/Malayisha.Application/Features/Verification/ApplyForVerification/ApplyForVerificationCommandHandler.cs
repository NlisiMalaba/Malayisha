using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Verification.ApplyForVerification;

internal sealed class ApplyForVerificationCommandHandler(
    ITransporterProfileRepository profileRepository,
    IVerificationRepository verificationRepository,
    TimeProvider timeProvider,
    ILogger<ApplyForVerificationCommandHandler> logger)
    : IRequestHandler<ApplyForVerificationCommand, Result<VerificationResponse>>
{
    public async Task<Result<VerificationResponse>> Handle(
        ApplyForVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.ProfileNotFound);
        }

        if (await verificationRepository.HasActiveForProfileAsync(profile.Id, cancellationToken))
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.ActiveVerificationExists);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var verification = Domain.Entities.Verification.Create(Guid.NewGuid(), profile.Id, nowUtc);

        await verificationRepository.AddAsync(verification, cancellationToken);
        await verificationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Verification {VerificationId} submitted for profile {ProfileId}",
            verification.Id,
            profile.Id);

        return Result<VerificationResponse>.Success(VerificationMappings.ToResponse(verification));
    }
}
