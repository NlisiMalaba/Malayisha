using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking.CancelBooking;

public sealed record CancelBookingCommand(
    Guid UserId,
    Guid BookingId,
    UserRole ActorRole) : IRequest<Result>;

internal sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();

        RuleFor(command => command.ActorRole)
            .Must(role => role is UserRole.Sender or UserRole.Transporter);
    }
}

internal sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    ILogger<CancelBookingCommandHandler> logger) : IRequestHandler<CancelBookingCommand, Result>
{
    public Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken) =>
        BookingTransitionExecutor.ExecuteAsync(
            bookingRepository,
            timeProvider,
            logger,
            request.BookingId,
            request.UserId,
            request.ActorRole,
            BookingStatus.Cancelled,
            null,
            false,
            cancellationToken);
}
