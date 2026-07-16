using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Malayisha.Application.Abstractions.Chat;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Chat;
using Malayisha.Application.Features.Chat.FlushUndelivered;
using Malayisha.Application.Features.Chat.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Malayisha.Api.Hubs;

[Authorize]
public sealed class ChatHub(
    IMediator mediator,
    IBookingRepository bookingRepository,
    IChatPresenceTracker chatPresenceTracker,
    ILogger<ChatHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (!TryGetUserId(out var userId))
        {
            Context.Abort();
            return;
        }

        await chatPresenceTracker.ConnectAsync(userId, Context.ConnectionId, Context.ConnectionAborted);

        var bookings = await bookingRepository.ListActiveByParticipantAsync(userId, Context.ConnectionAborted);
        foreach (var booking in bookings)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                GetBookingGroupName(booking.Id),
                Context.ConnectionAborted);
        }

        var flushResult = await mediator.Send(
            new FlushUndeliveredMessagesCommand(userId),
            Context.ConnectionAborted);

        if (flushResult.IsSuccess)
        {
            foreach (var message in flushResult.Value ?? [])
            {
                await Clients.Caller.SendAsync(
                    ChatConstants.ReceiveMessageMethod,
                    message,
                    Context.ConnectionAborted);
            }
        }
        else
        {
            logger.LogWarning(
                "Failed to flush undelivered chat messages for user {UserId}: {ErrorCode}",
                userId,
                flushResult.ErrorCode);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetUserId(out var userId))
        {
            await chatPresenceTracker.DisconnectAsync(
                userId,
                Context.ConnectionId,
                Context.ConnectionAborted);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid bookingId, string text)
    {
        if (!TryGetUserId(out var userId))
        {
            throw new HubException("Unauthorized");
        }

        try
        {
            var result = await mediator.Send(
                new SendMessageCommand(userId, bookingId, text),
                Context.ConnectionAborted);

            if (result.IsError)
            {
                throw ChatHubErrorMapper.ToHubException(result.ErrorCode!);
            }
        }
        catch (ValidationException)
        {
            throw ChatHubErrorMapper.ToHubException(ChatErrorCodes.MessageTooLong);
        }
    }

    public static string GetBookingGroupName(Guid bookingId) => $"booking:{bookingId}";

    private bool TryGetUserId(out Guid userId)
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }
}
