using FluentValidation;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Notifications.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(Guid UserId)
    : IRequest<Result<NotificationPreferencesResponse>>;

internal sealed class GetNotificationPreferencesQueryValidator
    : AbstractValidator<GetNotificationPreferencesQuery>
{
    public GetNotificationPreferencesQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}
