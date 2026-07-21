using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Notifications.GetNotificationPreferences;

internal sealed class GetNotificationPreferencesQueryHandler(IAuthRepository authRepository)
    : IRequestHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesResponse>>
{
    public async Task<Result<NotificationPreferencesResponse>> Handle(
        GetNotificationPreferencesQuery request,
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

        return Result<NotificationPreferencesResponse>.Success(
            new NotificationPreferencesResponse(user.MarketingNotificationsOptIn));
    }
}
