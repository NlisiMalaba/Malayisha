using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

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
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    ILogger<CompleteBookingCommandHandler> logger) : IRequestHandler<CompleteBookingCommand, Result>
{
    public Task<Result> Handle(CompleteBookingCommand request, CancellationToken cancellationToken) =>
        BookingTransitionExecutor.ExecuteAsync(
            bookingRepository,
            timeProvider,
            logger,
            request.BookingId,
            request.UserId,
            request.ActorRole,
            BookingStatus.Completed,
            null,
            request.IsSystemAction,
            cancellationToken);
}
