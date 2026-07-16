using FluentValidation;
using Malayisha.Application.Abstractions.Chat;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Chat.SendMessage;

public sealed record SendMessageCommand(
    Guid UserId,
    Guid BookingId,
    string Text) : IRequest<Result<ChatMessageDto>>;

internal sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.Text)
            .NotEmpty()
            .MaximumLength(ChatConstants.MaxMessageLength)
            .WithErrorCode(ChatErrorCodes.MessageTooLong);
    }
}

internal sealed class SendMessageCommandHandler(
    IBookingRepository bookingRepository,
    IChatMessageRepository chatMessageRepository,
    IChatPresenceTracker chatPresenceTracker,
    IChatNotifier chatNotifier,
    TimeProvider timeProvider,
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Result<ChatMessageDto>>
{
    public async Task<Result<ChatMessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.FindByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result<ChatMessageDto>.Error(ChatErrorCodes.BookingNotFound);
        }

        if (!ChatParticipantGuard.IsParticipant(booking, request.UserId))
        {
            return Result<ChatMessageDto>.Error(ChatErrorCodes.NotBookingParticipant);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var message = ChatMessage.Create(
            Guid.NewGuid(),
            request.BookingId,
            request.UserId,
            request.Text,
            nowUtc);

        await chatMessageRepository.AddAsync(message, cancellationToken);

        var recipientId = ChatParticipantGuard.GetOtherParticipantId(booking, request.UserId);
        if (await chatPresenceTracker.IsUserConnectedAsync(recipientId, cancellationToken))
        {
            message.MarkDelivered();
        }

        await chatMessageRepository.SaveChangesAsync(cancellationToken);

        var dto = ToDto(message);
        await chatNotifier.NotifyMessageAsync(request.BookingId, dto, cancellationToken);

        logger.LogInformation(
            "Chat message {MessageId} sent for booking {BookingId} by {SenderUserId}",
            message.Id,
            message.BookingId,
            message.SenderUserId);

        return Result<ChatMessageDto>.Success(dto);
    }

    internal static ChatMessageDto ToDto(ChatMessage message) =>
        new(message.Id, message.BookingId, message.SenderUserId, message.Text, message.SentAtUtc);
}
