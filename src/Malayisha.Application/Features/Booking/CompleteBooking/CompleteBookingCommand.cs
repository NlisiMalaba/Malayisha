using FluentValidation;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Booking.CompleteBooking;

public sealed record CompleteBookingCommand(
    Guid UserId,
    Guid BookingId,
    UserRole ActorRole = UserRole.Sender,
    bool IsSystemAction = false) : IRequest<Result>;

internal sealed class CompleteBookingCommandValidator : AbstractValidator<CompleteBookingCommand>
{
    public CompleteBookingCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();

        RuleFor(command => command.ActorRole)
            .Must(role => role is UserRole.Sender or UserRole.Admin);
    }
}

internal sealed class CompleteBookingCommandHandler(
    IBookingTransitionService bookingTransitionService) : IRequestHandler<CompleteBookingCommand, Result>
{
    public Task<Result> Handle(CompleteBookingCommand request, CancellationToken cancellationToken) =>
        bookingTransitionService.ExecuteAsync(
            request.BookingId,
            request.UserId,
            request.ActorRole,
            BookingStatus.Completed,
            null,
            request.IsSystemAction,
            cancellationToken);
}
