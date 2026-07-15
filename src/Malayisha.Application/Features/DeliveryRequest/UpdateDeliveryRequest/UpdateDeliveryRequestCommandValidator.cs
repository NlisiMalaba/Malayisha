using FluentValidation;

namespace Malayisha.Application.Features.DeliveryRequest.UpdateDeliveryRequest;

internal sealed class UpdateDeliveryRequestCommandValidator : AbstractValidator<UpdateDeliveryRequestCommand>
{
    public UpdateDeliveryRequestCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.DeliveryRequestId)
            .NotEmpty();

        RuleFor(command => command.OriginCity)
            .NotEmpty()
            .MaximumLength(DeliveryRequestValidation.CityMaxLength);

        RuleFor(command => command.DestinationCity)
            .NotEmpty()
            .MaximumLength(DeliveryRequestValidation.CityMaxLength);

        RuleFor(command => command.WeightKg)
            .GreaterThan(0);

        RuleFor(command => command.SizeDescription)
            .NotEmpty()
            .MaximumLength(DeliveryRequestValidation.SizeDescriptionMaxLength);

        RuleFor(command => command.GoodsDescription)
            .NotEmpty()
            .MaximumLength(DeliveryRequestValidation.GoodsDescriptionMaxLength);
    }
}
