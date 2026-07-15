using FluentValidation;

namespace Malayisha.Application.Features.DeliveryRequest.CreateDeliveryRequest;

internal sealed class CreateDeliveryRequestCommandValidator : AbstractValidator<CreateDeliveryRequestCommand>
{
    public CreateDeliveryRequestCommandValidator()
    {
        RuleFor(command => command.UserId)
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
