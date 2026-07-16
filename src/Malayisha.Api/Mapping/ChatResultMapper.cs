using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Chat;
using Malayisha.Application.Common;
using ApplicationChatMessageDto = Malayisha.Application.Features.Chat.ChatMessageDto;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class ChatResultMapper
{
    public static IActionResult ToMessageHistoryResult(Result<IReadOnlyList<ApplicationChatMessageDto>> result) =>
        result.IsSuccess
            ? new OkObjectResult(new ChatMessageHistoryResponse(
                (result.Value ?? []).Select(ToDto).ToArray()))
            : ToErrorResult(result.ErrorCode);

    private static ChatMessageDto ToDto(ApplicationChatMessageDto message) =>
        new(
            message.Id,
            message.BookingId,
            message.SenderUserId,
            message.Text,
            message.SentAtUtc);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = ChatErrorMapper.ToStatusCode(errorCode)
        };
}
