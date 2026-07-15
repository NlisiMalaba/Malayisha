using FluentValidation;

namespace Malayisha.Application.Features.DeliveryRequest.CancelDeliveryRequest;

internal sealed class CancelDeliveryRequestCommandValidator : AbstractValidator<CancelDeliveryRequestCommand>
{
    public CancelDeliveryRequestCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.DeliveryRequestId)
            .NotEmpty();
    }
}
