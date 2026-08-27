using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Notifications.RegisterPushDeviceToken;

internal sealed class RegisterPushDeviceTokenCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    ILogger<RegisterPushDeviceTokenCommandHandler> logger)
    : IRequestHandler<RegisterPushDeviceTokenCommand, Result<PushDeviceTokenResponse>>
{
    public async Task<Result<PushDeviceTokenResponse>> Handle(
        RegisterPushDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var user = await authRepository.FindUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<PushDeviceTokenResponse>.Error(NotificationErrorCodes.UserNotFound);
        }

        if (!user.IsActive)
        {
            return Result<PushDeviceTokenResponse>.Error(NotificationErrorCodes.UserInactive);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        user.UpdatePushDeviceToken(request.DeviceToken, nowUtc);

        await authRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Registered push device token for user {UserId}",
            user.Id);

        return Result<PushDeviceTokenResponse>.Success(new PushDeviceTokenResponse(true));
    }
}
