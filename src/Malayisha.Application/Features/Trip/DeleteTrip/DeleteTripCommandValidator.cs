using FluentValidation;

namespace Malayisha.Application.Features.Trip.DeleteTrip;

internal sealed class DeleteTripCommandValidator : AbstractValidator<DeleteTripCommand>
{
    public DeleteTripCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.TripListingId)
            .NotEmpty();
    }
}
