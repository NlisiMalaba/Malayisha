using FluentValidation;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;

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
    IBookingTransitionService bookingTransitionService) : IRequestHandler<CancelBookingCommand, Result>
{
    public Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken) =>
        bookingTransitionService.ExecuteAsync(
            request.BookingId,
            request.UserId,
            request.ActorRole,
            BookingStatus.Cancelled,
            null,
            false,
            cancellationToken);
}
