using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Notifications.UpdateNotificationPreferences;

internal sealed class UpdateNotificationPreferencesCommandHandler(
    IAuthRepository authRepository,
    TimeProvider timeProvider,
    ILogger<UpdateNotificationPreferencesCommandHandler> logger)
    : IRequestHandler<UpdateNotificationPreferencesCommand, Result<NotificationPreferencesResponse>>
{
    public async Task<Result<NotificationPreferencesResponse>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var user = await authRepository.FindUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<NotificationPreferencesResponse>.Error(NotificationErrorCodes.UserNotFound);
        }

        if (!user.IsActive)
        {
            return Result<NotificationPreferencesResponse>.Error(NotificationErrorCodes.UserInactive);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        user.SetMarketingNotificationsOptIn(request.MarketingNotificationsOptIn, nowUtc);

        await authRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated notification preferences for user {UserId}. Marketing opt-in: {MarketingOptIn}",
            user.Id,
            user.MarketingNotificationsOptIn);

        return Result<NotificationPreferencesResponse>.Success(
            new NotificationPreferencesResponse(user.MarketingNotificationsOptIn));
    }
}
