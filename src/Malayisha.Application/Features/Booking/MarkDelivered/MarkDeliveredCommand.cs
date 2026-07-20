using FluentValidation;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Booking.MarkDelivered;

public sealed record MarkDeliveredCommand(
    Guid UserId,
    Guid BookingId) : IRequest<Result>;

internal sealed class MarkDeliveredCommandValidator : AbstractValidator<MarkDeliveredCommand>
{
    public MarkDeliveredCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();
    }
}

internal sealed class MarkDeliveredCommandHandler(
    IBookingTransitionService bookingTransitionService) : IRequestHandler<MarkDeliveredCommand, Result>
{
    public Task<Result> Handle(MarkDeliveredCommand request, CancellationToken cancellationToken) =>
        bookingTransitionService.ExecuteAsync(
            request.BookingId,
            request.UserId,
            UserRole.Transporter,
            BookingStatus.Delivered,
            null,
            false,
            cancellationToken);
}
