using FluentValidation;

namespace Malayisha.Application.Features.Booking.GetBooking;

internal sealed class GetBookingQueryValidator : AbstractValidator<GetBookingQuery>
{
    public GetBookingQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty();

        RuleFor(query => query.BookingId)
            .NotEmpty();
    }
}
