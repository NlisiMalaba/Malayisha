using FluentValidation;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Notifications.RegisterPushDeviceToken;

public sealed record RegisterPushDeviceTokenCommand(
    Guid UserId,
    string DeviceToken) : IRequest<Result<PushDeviceTokenResponse>>;

internal sealed class RegisterPushDeviceTokenCommandValidator
    : AbstractValidator<RegisterPushDeviceTokenCommand>
{
    public const int MaxDeviceTokenLength = 512;

    public RegisterPushDeviceTokenCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DeviceToken)
            .NotEmpty()
            .MaximumLength(MaxDeviceTokenLength);
    }
}
