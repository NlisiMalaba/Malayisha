using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking.ConfirmBooking;

public sealed record ConfirmBookingCommand(
    Guid UserId,
    Guid BookingId,
    decimal AgreedPriceZar) : IRequest<Result>;

internal sealed class ConfirmBookingCommandValidator : AbstractValidator<ConfirmBookingCommand>
{
    public ConfirmBookingCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.AgreedPriceZar).GreaterThan(0);
    }
}

internal sealed class ConfirmBookingCommandHandler(
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    ILogger<ConfirmBookingCommandHandler> logger) : IRequestHandler<ConfirmBookingCommand, Result>
{
    public Task<Result> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken) =>
        BookingTransitionExecutor.ExecuteAsync(
            bookingRepository,
            timeProvider,
            logger,
            request.BookingId,
            request.UserId,
            UserRole.Sender,
            BookingStatus.Confirmed,
            request.AgreedPriceZar,
            false,
            cancellationToken);
}
