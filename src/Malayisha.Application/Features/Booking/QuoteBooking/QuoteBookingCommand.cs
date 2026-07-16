using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

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
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    ILogger<QuoteBookingCommandHandler> logger) : IRequestHandler<QuoteBookingCommand, Result>
{
    public Task<Result> Handle(QuoteBookingCommand request, CancellationToken cancellationToken) =>
        BookingTransitionExecutor.ExecuteAsync(
            bookingRepository,
            timeProvider,
            logger,
            request.BookingId,
            request.UserId,
            UserRole.Transporter,
            BookingStatus.Quoted,
            request.QuotedPriceZar,
            false,
            cancellationToken);
}
