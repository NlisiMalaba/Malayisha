using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

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
    IBookingRepository bookingRepository,
    TimeProvider timeProvider,
    ILogger<MarkDeliveredCommandHandler> logger) : IRequestHandler<MarkDeliveredCommand, Result>
{
    public Task<Result> Handle(MarkDeliveredCommand request, CancellationToken cancellationToken) =>
        BookingTransitionExecutor.ExecuteAsync(
            bookingRepository,
            timeProvider,
            logger,
            request.BookingId,
            request.UserId,
            UserRole.Transporter,
            BookingStatus.Delivered,
            null,
            false,
            cancellationToken);
}
