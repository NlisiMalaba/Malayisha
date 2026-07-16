using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking.MarkInTransit;

public sealed record MarkInTransitCommand(
    Guid UserId,
    Guid BookingId) : IRequest<Result>;

internal sealed class MarkInTransitCommandValidator : AbstractValidator<MarkInTransitCommand>
{
    public MarkInTransitCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();
    }
}

internal sealed class MarkInTransitCommandHandler(
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    ILogger<MarkInTransitCommandHandler> logger) : IRequestHandler<MarkInTransitCommand, Result>
{
    public Task<Result> Handle(MarkInTransitCommand request, CancellationToken cancellationToken) =>
        BookingTransitionExecutor.ExecuteAsync(
            bookingRepository,
            timeProvider,
            logger,
            request.BookingId,
            request.UserId,
            UserRole.Transporter,
            BookingStatus.InTransit,
            null,
            false,
            cancellationToken);
}
