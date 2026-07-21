using FluentValidation;

namespace Malayisha.Application.Features.Booking.ListBookings;

internal sealed class ListBookingsQueryValidator : AbstractValidator<ListBookingsQuery>
{
    public ListBookingsQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty();

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, BookingValidation.MaxPageSize);
    }
}
