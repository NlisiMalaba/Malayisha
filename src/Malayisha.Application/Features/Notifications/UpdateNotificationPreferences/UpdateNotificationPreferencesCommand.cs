using FluentValidation;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Notifications.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid UserId,
    bool MarketingNotificationsOptIn) : IRequest<Result<NotificationPreferencesResponse>>;

internal sealed class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
    }
}
