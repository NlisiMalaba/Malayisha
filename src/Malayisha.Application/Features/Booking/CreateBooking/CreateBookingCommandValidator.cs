using FluentValidation;

namespace Malayisha.Application.Features.Booking.CreateBooking;

internal sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(command => command.SenderId)
            .NotEmpty();

        RuleFor(command => command.TripListingId)
            .NotEmpty();

        RuleFor(command => command.DeliveryRequestId)
            .NotEqual(Guid.Empty)
            .When(command => command.DeliveryRequestId.HasValue);

        RuleFor(command => command.Message)
            .MaximumLength(2000);
    }
}
