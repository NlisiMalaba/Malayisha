using FluentValidation;
using Malayisha.Application.Features.Chat;
using Malayisha.Application.Features.Chat.FlushUndelivered;
using Malayisha.Application.Features.Chat.SendMessage;
using Malayisha.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class ChatOfflineQueuingTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SendMessageValidator_Accepts2000Characters()
    {
        var validator = new SendMessageCommandValidator();
        var text = new string('x', ChatConstants.MaxMessageLength);

        var result = validator.Validate(
            new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), text));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SendMessageValidator_Rejects2001Characters()
    {
        var validator = new SendMessageCommandValidator();
        var text = new string('x', ChatConstants.MaxMessageLength + 1);

        var result = validator.Validate(
            new SendMessageCommand(Guid.NewGuid(), Guid.NewGuid(), text));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorCode == ChatErrorCodes.MessageTooLong);
    }

    [Fact]
    public async Task SendMessage_SetsIsDeliveredFalse_WhenRecipientOffline()
    {
        var harness = CreateHarness(recipientOnline: false);

        var result = await harness.SendHandler.Handle(
            new SendMessageCommand(harness.SenderId, harness.BookingId, "Hello offline"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(harness.Messages.Items);
        Assert.False(harness.Messages.Items[0].IsDelivered);
    }

    [Fact]
    public async Task SendMessage_SetsIsDeliveredTrue_WhenRecipientOnline()
    {
        var harness = CreateHarness(recipientOnline: true);

        var result = await harness.SendHandler.Handle(
            new SendMessageCommand(harness.SenderId, harness.BookingId, "Hello online"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(harness.Messages.Items);
        Assert.True(harness.Messages.Items[0].IsDelivered);
    }

    [Fact]
    public async Task FlushUndeliveredMessages_DeliversQueuedMessagesOnReconnect()
    {
        var harness = CreateHarness(recipientOnline: false);

        await harness.SendHandler.Handle(
            new SendMessageCommand(harness.SenderId, harness.BookingId, "First queued"),
            CancellationToken.None);
        await harness.SendHandler.Handle(
            new SendMessageCommand(harness.SenderId, harness.BookingId, "Second queued"),
            CancellationToken.None);

        Assert.All(harness.Messages.Items, message => Assert.False(message.IsDelivered));

        var flush = await harness.FlushHandler.Handle(
            new FlushUndeliveredMessagesCommand(harness.TransporterId),
            CancellationToken.None);

        Assert.True(flush.IsSuccess);
        Assert.NotNull(flush.Value);
        Assert.Equal(2, flush.Value.Count);
        Assert.Equal("First queued", flush.Value[0].Text);
        Assert.Equal("Second queued", flush.Value[1].Text);
        Assert.All(harness.Messages.Items, message => Assert.True(message.IsDelivered));

        var secondFlush = await harness.FlushHandler.Handle(
            new FlushUndeliveredMessagesCommand(harness.TransporterId),
            CancellationToken.None);

        Assert.True(secondFlush.IsSuccess);
        Assert.NotNull(secondFlush.Value);
        Assert.Empty(secondFlush.Value);
    }

    private static OfflineQueuingHarness CreateHarness(bool recipientOnline)
    {
        var senderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var transporterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var bookingId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var booking = Booking.Create(
            bookingId,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            senderId,
            transporterId,
            BaselineUtc);

        var bookings = new ChatPropertyTests.InMemoryBookingRepository([booking]);
        var messages = new ChatPropertyTests.InMemoryChatMessageRepository(bookings);
        var presence = new ChatPropertyTests.FakeChatPresenceTracker();
        var notifier = new ChatPropertyTests.FakeChatNotifier();

        if (recipientOnline)
        {
            presence.SetConnected(transporterId, true);
        }

        return new OfflineQueuingHarness(
            senderId,
            transporterId,
            bookingId,
            messages,
            new SendMessageCommandHandler(
                bookings,
                messages,
                presence,
                notifier,
                TimeProvider.System,
                NullLogger<SendMessageCommandHandler>.Instance),
            new FlushUndeliveredMessagesCommandHandler(
                messages,
                NullLogger<FlushUndeliveredMessagesCommandHandler>.Instance));
    }

    private sealed class OfflineQueuingHarness(
        Guid senderId,
        Guid transporterId,
        Guid bookingId,
        ChatPropertyTests.InMemoryChatMessageRepository messages,
        SendMessageCommandHandler sendHandler,
        FlushUndeliveredMessagesCommandHandler flushHandler)
    {
        public Guid SenderId { get; } = senderId;
        public Guid TransporterId { get; } = transporterId;
        public Guid BookingId { get; } = bookingId;
        public ChatPropertyTests.InMemoryChatMessageRepository Messages { get; } = messages;
        public SendMessageCommandHandler SendHandler { get; } = sendHandler;
        public FlushUndeliveredMessagesCommandHandler FlushHandler { get; } = flushHandler;
    }
}
