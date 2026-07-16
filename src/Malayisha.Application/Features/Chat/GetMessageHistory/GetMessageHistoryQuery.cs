using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Chat.SendMessage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Chat.GetMessageHistory;

public sealed record GetMessageHistoryQuery(
    Guid UserId,
    Guid BookingId) : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

internal sealed class GetMessageHistoryQueryValidator : AbstractValidator<GetMessageHistoryQuery>
{
    public GetMessageHistoryQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
        RuleFor(query => query.BookingId).NotEmpty();
    }
}

internal sealed class GetMessageHistoryQueryHandler(
    IBookingRepository bookingRepository,
    IChatMessageRepository chatMessageRepository,
    ILogger<GetMessageHistoryQueryHandler> logger)
    : IRequestHandler<GetMessageHistoryQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(
        GetMessageHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.FindByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result<IReadOnlyList<ChatMessageDto>>.Error(ChatErrorCodes.BookingNotFound);
        }

        if (!ChatParticipantGuard.IsParticipant(booking, request.UserId))
        {
            return Result<IReadOnlyList<ChatMessageDto>>.Error(ChatErrorCodes.NotBookingParticipant);
        }

        var messages = await chatMessageRepository.ListByBookingIdAsync(request.BookingId, cancellationToken);
        var dtos = messages
            .Select(SendMessageCommandHandler.ToDto)
            .ToList();

        logger.LogInformation(
            "Retrieved {MessageCount} chat messages for booking {BookingId} by user {UserId}",
            dtos.Count,
            request.BookingId,
            request.UserId);

        return Result<IReadOnlyList<ChatMessageDto>>.Success(dtos);
    }
}
