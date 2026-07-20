using FluentValidation;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Booking.QuoteBooking;

public sealed record QuoteBookingCommand(
    Guid UserId,
    Guid BookingId,
    decimal QuotedPriceZar) : IRequest<Result>;

internal sealed class QuoteBookingCommandValidator : AbstractValidator<QuoteBookingCommand>
{
    public QuoteBookingCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.QuotedPriceZar).GreaterThan(0);
    }
}

internal sealed class QuoteBookingCommandHandler(
    IBookingTransitionService bookingTransitionService) : IRequestHandler<QuoteBookingCommand, Result>
{
    public Task<Result> Handle(QuoteBookingCommand request, CancellationToken cancellationToken) =>
        bookingTransitionService.ExecuteAsync(
            request.BookingId,
            request.UserId,
            UserRole.Transporter,
            BookingStatus.Quoted,
            request.QuotedPriceZar,
            false,
            cancellationToken);
}
