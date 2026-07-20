using FluentValidation;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;

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
    IBookingTransitionService bookingTransitionService) : IRequestHandler<MarkInTransitCommand, Result>
{
    public Task<Result> Handle(MarkInTransitCommand request, CancellationToken cancellationToken) =>
        bookingTransitionService.ExecuteAsync(
            request.BookingId,
            request.UserId,
            UserRole.Transporter,
            BookingStatus.InTransit,
            null,
            false,
            cancellationToken);
}
