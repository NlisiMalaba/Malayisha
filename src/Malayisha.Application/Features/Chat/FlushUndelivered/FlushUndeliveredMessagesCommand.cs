using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Chat.SendMessage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Chat.FlushUndelivered;

public sealed record FlushUndeliveredMessagesCommand(Guid UserId)
    : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

internal sealed class FlushUndeliveredMessagesCommandHandler(
    IChatMessageRepository chatMessageRepository,
    ILogger<FlushUndeliveredMessagesCommandHandler> logger)
    : IRequestHandler<FlushUndeliveredMessagesCommand, Result<IReadOnlyList<ChatMessageDto>>>
{
    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(
        FlushUndeliveredMessagesCommand request,
        CancellationToken cancellationToken)
    {
        var messages = await chatMessageRepository.ListUndeliveredForRecipientAsync(
            request.UserId,
            cancellationToken);

        if (messages.Count == 0)
        {
            return Result<IReadOnlyList<ChatMessageDto>>.Success([]);
        }

        foreach (var message in messages)
        {
            message.MarkDelivered();
        }

        await chatMessageRepository.SaveChangesAsync(cancellationToken);

        var dtos = messages
            .Select(SendMessageCommandHandler.ToDto)
            .ToList();

        logger.LogInformation(
            "Flushed {MessageCount} undelivered chat messages for user {UserId}",
            dtos.Count,
            request.UserId);

        return Result<IReadOnlyList<ChatMessageDto>>.Success(dtos);
    }
}
