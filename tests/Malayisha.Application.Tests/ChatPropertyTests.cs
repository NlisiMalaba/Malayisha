using System.Globalization;
using FluentValidation;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Chat;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Chat;
using Malayisha.Application.Features.Chat.FlushUndelivered;
using Malayisha.Application.Features.Chat.GetMessageHistory;
using Malayisha.Application.Features.Chat.SendMessage;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class ChatPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool Property25_SendMessage_RoundTripsWithCorrectOrdering(
        int senderSeed,
        int transporterSeed,
        int bookingSeed,
        int textSeed,
        int messageCountSeed)
    {
        return RunMessageRoundTripAsync(
            senderSeed,
            transporterSeed,
            bookingSeed,
            textSeed,
            messageCountSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property26_ChatAccessRestrictedToBookingParticipants(
        int senderSeed,
        int transporterSeed,
        int outsiderSeed,
        int bookingSeed,
        int textSeed)
    {
        return RunAccessRestrictionAsync(
            senderSeed,
            transporterSeed,
            outsiderSeed,
            bookingSeed,
            textSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunMessageRoundTripAsync(
        int senderSeed,
        int transporterSeed,
        int bookingSeed,
        int textSeed,
        int messageCountSeed)
    {
        var harness = ChatTestHarness.Create(senderSeed, transporterSeed, bookingSeed);
        var messageCount = (Math.Abs(messageCountSeed) % 5) + 1;
        var sentTexts = new List<string>();

        for (var index = 0; index < messageCount; index++)
        {
            var text = BuildMessageText(textSeed + index, ChatConstants.MaxMessageLength);
            sentTexts.Add(text);

            var send = await harness.SendHandler.Handle(
                new SendMessageCommand(harness.SenderId, harness.BookingId, text),
                CancellationToken.None);

            if (send.IsError || send.Value is null)
            {
                return false;
            }

            if (send.Value.BookingId != harness.BookingId
                || send.Value.SenderUserId != harness.SenderId
                || send.Value.Text != text
                || send.Value.SentAtUtc == default)
            {
                return false;
            }
        }

        var history = await harness.HistoryHandler.Handle(
            new GetMessageHistoryQuery(harness.TransporterId, harness.BookingId),
            CancellationToken.None);

        if (history.IsError || history.Value is null || history.Value.Count != messageCount)
        {
            return false;
        }

        for (var index = 1; index < history.Value.Count; index++)
        {
            if (history.Value[index].SentAtUtc < history.Value[index - 1].SentAtUtc)
            {
                return false;
            }
        }

        return history.Value.Select(message => message.Text).SequenceEqual(sentTexts);
    }

    private static async Task<bool> RunAccessRestrictionAsync(
        int senderSeed,
        int transporterSeed,
        int outsiderSeed,
        int bookingSeed,
        int textSeed)
    {
        var harness = ChatTestHarness.Create(senderSeed, transporterSeed, bookingSeed);
        var outsiderId = BuildUserId(outsiderSeed);

        if (outsiderId == harness.SenderId || outsiderId == harness.TransporterId)
        {
            return true;
        }

        var text = BuildMessageText(textSeed, ChatConstants.MaxMessageLength);

        var history = await harness.HistoryHandler.Handle(
            new GetMessageHistoryQuery(outsiderId, harness.BookingId),
            CancellationToken.None);

        if (!history.IsError || history.ErrorCode != ChatErrorCodes.NotBookingParticipant)
        {
            return false;
        }

        var send = await harness.SendHandler.Handle(
            new SendMessageCommand(outsiderId, harness.BookingId, text),
            CancellationToken.None);

        return send.IsError && send.ErrorCode == ChatErrorCodes.NotBookingParticipant;
    }

    private static string BuildMessageText(int seed, int maxLength)
    {
        var length = (Math.Abs(seed) % maxLength) + 1;
        var prefix = $"msg-{Math.Abs(seed).ToString(CultureInfo.InvariantCulture)}-";
        if (prefix.Length >= length)
        {
            return prefix[..length];
        }

        return prefix + new string('a', length - prefix.Length);
    }

    private static Guid BuildUserId(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private sealed class ChatTestHarness
    {
        private ChatTestHarness(
            Guid senderId,
            Guid transporterId,
            Guid bookingId,
            InMemoryBookingRepository bookings,
            InMemoryChatMessageRepository messages,
            SendMessageCommandHandler sendHandler,
            GetMessageHistoryQueryHandler historyHandler)
        {
            SenderId = senderId;
            TransporterId = transporterId;
            BookingId = bookingId;
            Bookings = bookings;
            Messages = messages;
            SendHandler = sendHandler;
            HistoryHandler = historyHandler;
        }

        public Guid SenderId { get; }
        public Guid TransporterId { get; }
        public Guid BookingId { get; }
        public InMemoryBookingRepository Bookings { get; }
        public InMemoryChatMessageRepository Messages { get; }
        public SendMessageCommandHandler SendHandler { get; }
        public GetMessageHistoryQueryHandler HistoryHandler { get; }

        public static ChatTestHarness Create(int senderSeed, int transporterSeed, int bookingSeed)
        {
            var senderId = BuildUserId(senderSeed);
            var transporterId = BuildUserId(transporterSeed);

            if (transporterId == senderId)
            {
                transporterId = BuildUserId(transporterSeed ^ 0x13579BDF);
            }

            var bookingId = BuildUserId(bookingSeed);
            var booking = Booking.Create(
                bookingId,
                BuildUserId(bookingSeed ^ 0x2468ACE0),
                senderId,
                transporterId,
                BaselineUtc);

            var bookings = new InMemoryBookingRepository([booking]);
            var messages = new InMemoryChatMessageRepository(bookings);
            var presence = new FakeChatPresenceTracker();
            var notifier = new FakeChatNotifier();
            var clock = new IncrementingTimeProvider(BaselineUtc);

            return new ChatTestHarness(
                senderId,
                transporterId,
                bookingId,
                bookings,
                messages,
                new SendMessageCommandHandler(
                    bookings,
                    messages,
                    presence,
                    notifier,
                    clock,
                    NullLogger<SendMessageCommandHandler>.Instance),
                new GetMessageHistoryQueryHandler(
                    bookings,
                    messages,
                    NullLogger<GetMessageHistoryQueryHandler>.Instance));
        }
    }

    internal sealed class InMemoryChatMessageRepository : IChatMessageRepository
    {
        private readonly InMemoryBookingRepository _bookingRepository;
        private readonly List<ChatMessage> _messages = [];

        public InMemoryChatMessageRepository(InMemoryBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public IReadOnlyList<ChatMessage> Items => _messages;

        public Task<IReadOnlyList<ChatMessage>> ListUndeliveredForRecipientAsync(
            Guid recipientUserId,
            CancellationToken cancellationToken = default)
        {
            var items = _messages
                .Where(message =>
                    !message.IsDelivered
                    && message.SenderUserId != recipientUserId
                    && IsActiveParticipantBooking(message.BookingId, recipientUserId))
                .OrderBy(message => message.SentAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<ChatMessage>>(items);
        }

        public Task<IReadOnlyList<ChatMessage>> ListByBookingIdAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            var items = _messages
                .Where(message => message.BookingId == bookingId)
                .OrderBy(message => message.SentAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<ChatMessage>>(items);
        }

        public Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private bool IsActiveParticipantBooking(Guid bookingId, Guid userId)
        {
            var booking = _bookingRepository.FindByIdAsync(bookingId).GetAwaiter().GetResult();
            return booking is not null
                   && (booking.SenderId == userId || booking.TransporterId == userId)
                   && booking.Status != BookingStatus.Completed
                   && booking.Status != BookingStatus.Cancelled;
        }
    }

    internal sealed class InMemoryBookingRepository : IBookingRepository
    {
        private readonly Dictionary<Guid, Booking> _bookings;

        public InMemoryBookingRepository(IEnumerable<Booking> bookings) =>
            _bookings = bookings.ToDictionary(booking => booking.Id);

        public Task<Booking?> FindByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_bookings.TryGetValue(bookingId, out var booking) ? booking : null);

        public Task<IReadOnlyList<Booking>> ListActiveByParticipantAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var items = _bookings.Values
                .Where(booking =>
                    (booking.SenderId == userId || booking.TransporterId == userId)
                    && booking.Status != BookingStatus.Completed
                    && booking.Status != BookingStatus.Cancelled)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Booking>>(items);
        }

        public Task<IReadOnlyList<Booking>> ListDeliveredBeforeAsync(
            DateTime deliveredBeforeUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Booking>>([]);

        public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            _bookings[booking.Id] = booking;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    internal sealed class FakeChatPresenceTracker : IChatPresenceTracker
    {
        private readonly HashSet<Guid> _connectedUsers = [];

        public void SetConnected(Guid userId, bool connected)
        {
            if (connected)
            {
                _connectedUsers.Add(userId);
            }
            else
            {
                _connectedUsers.Remove(userId);
            }
        }

        public Task ConnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
        {
            _connectedUsers.Add(userId);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
        {
            _connectedUsers.Remove(userId);
            return Task.CompletedTask;
        }

        public Task<bool> IsUserConnectedAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_connectedUsers.Contains(userId));
    }

    internal sealed class FakeChatNotifier : IChatNotifier
    {
        public List<(Guid BookingId, ChatMessageDto Message)> Notifications { get; } = [];

        public Task NotifyMessageAsync(
            Guid bookingId,
            ChatMessageDto message,
            CancellationToken cancellationToken = default)
        {
            Notifications.Add((bookingId, message));
            return Task.CompletedTask;
        }
    }

    private sealed class IncrementingTimeProvider : TimeProvider
    {
        private DateTimeOffset _current;

        public IncrementingTimeProvider(DateTime utcStart) =>
            _current = new DateTimeOffset(utcStart, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            var result = _current;
            _current = _current.AddMilliseconds(1);
            return result;
        }
    }
}
