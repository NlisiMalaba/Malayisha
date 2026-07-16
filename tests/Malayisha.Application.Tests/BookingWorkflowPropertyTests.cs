using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Booking.CompleteBooking;
using Malayisha.Application.Features.Booking.CreateBooking;
using Malayisha.Domain.Common;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Jobs;
using Malayisha.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Tests;

public sealed class BookingWorkflowPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 120)]
    public bool Property22_CreateBooking_AlwaysStartsAtRequested(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        bool includeDeliveryRequest)
    {
        return RunCreateStartsRequestedAsync(senderSeed, transporterSeed, tripSeed, includeDeliveryRequest)
            .GetAwaiter()
            .GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property24_AutoComplete_CreatesSingleCommissionRecord_Idempotent(int amountSeed)
    {
        return RunAutoCompleteIdempotencyAsync(amountSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunCreateStartsRequestedAsync(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        bool includeDeliveryRequest)
    {
        var senderId = BuildUserId(senderSeed);
        var transporterId = BuildUserId(transporterSeed == senderSeed ? transporterSeed + 1 : transporterSeed);
        var tripId = BuildGuid(tripSeed);
        var profileId = BuildGuid(tripSeed ^ 0x13579BDF);
        var deliveryRequestId = includeDeliveryRequest ? BuildGuid(tripSeed ^ 0x2468ACE0) : (Guid?)null;
        var clock = new FixedTimeProvider(BaselineUtc);

        var trip = TripListing.Create(
            tripId,
            profileId,
            "Johannesburg",
            "Harare",
            BaselineUtc.AddDays(5),
            200m,
            600m,
            BaselineUtc);

        var profile = TransporterProfile.Create(
            profileId,
            transporterId,
            "Transporter",
            ["JHB-HRE"],
            "Van",
            500m,
            BaselineUtc);

        var deliveryRequest = includeDeliveryRequest
            ? DeliveryRequest.Create(
                deliveryRequestId!.Value,
                senderId,
                "Johannesburg",
                "Harare",
                BaselineUtc.AddDays(4),
                20m,
                "Box",
                "Electronics",
                BaselineUtc)
            : null;

        var bookings = new InMemoryBookingRepository();
        var requests = new InMemoryDeliveryRequestRepository(deliveryRequest);
        var handler = new CreateBookingCommandHandler(
            bookings,
            new InMemoryTripListingRepository(trip),
            new InMemoryTransporterProfileRepository(profile),
            requests,
            clock,
            NullLogger<CreateBookingCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateBookingCommand(senderId, tripId, deliveryRequestId, "Please handle with care"),
            CancellationToken.None);

        if (result.IsError || result.Value == Guid.Empty)
        {
            return false;
        }

        var created = await bookings.FindByIdAsync(result.Value);
        if (created is null)
        {
            return false;
        }

        var request = deliveryRequestId.HasValue
            ? await requests.FindByIdAsync(deliveryRequestId.Value)
            : null;

        return created.Status == BookingStatus.Requested
               && created.SenderId == senderId
               && created.TransporterId == transporterId
               && created.TripListingId == tripId
               && created.DeliveryRequestId == deliveryRequestId
               && (!includeDeliveryRequest || request?.Status == DeliveryRequestStatus.ConvertedToBooking);
    }

    private static async Task<bool> RunAutoCompleteIdempotencyAsync(int amountSeed)
    {
        var senderId = BuildUserId(101);
        var transporterId = BuildUserId(202);
        var agreed = (Math.Abs(amountSeed) % 5000) + 100m;
        var bookingId = Guid.NewGuid();
        var deliveredAt = BaselineUtc.AddHours(-60);

        var booking = Booking.Create(
            bookingId,
            Guid.NewGuid(),
            senderId,
            transporterId,
            BaselineUtc.AddHours(-65),
            "seed");
        _ = booking.Transition(BookingStatus.Quoted, transporterId, UserRole.Transporter, BaselineUtc.AddHours(-64), agreed - 10m);
        _ = booking.Transition(BookingStatus.Confirmed, senderId, UserRole.Sender, BaselineUtc.AddHours(-63), agreed);
        _ = booking.Transition(BookingStatus.InTransit, transporterId, UserRole.Transporter, BaselineUtc.AddHours(-62));
        _ = booking.Transition(BookingStatus.Delivered, transporterId, UserRole.Transporter, deliveredAt);

        var bookingRepo = new InMemoryBookingRepository([booking]);
        var commissionRepo = new InMemoryCommissionRecordRepository();
        var clock = new FixedTimeProvider(BaselineUtc);
        var mediator = new InMemoryJobMediator(bookingRepo, clock);
        var options = Microsoft.Extensions.Options.Options.Create(new BookingWorkflowOptions
        {
            AutoCompleteAfterHours = 48,
            CommissionRate = 0.10m
        });

        var job = new AutoCompleteExpiredBookingsJob(
            bookingRepo,
            commissionRepo,
            mediator,
            clock,
            options,
            NullLogger<AutoCompleteExpiredBookingsJob>.Instance);

        await job.ExecuteAsync(CancellationToken.None);
        await job.ExecuteAsync(CancellationToken.None);

        var updated = await bookingRepo.FindByIdAsync(bookingId);
        var commissions = commissionRepo.Items.Where(item => item.BookingId == bookingId).ToArray();

        return updated is not null
               && updated.Status == BookingStatus.Completed
               && updated.CompletedAtUtc.HasValue
               && commissions.Length == 1
               && commissions[0].AgreedPriceZar == agreed
               && commissions[0].CommissionRate == 0.10m;
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

    private static Guid BuildGuid(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed * 7);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed ^ 0x01020304);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InMemoryBookingRepository : IBookingRepository
    {
        private readonly Dictionary<Guid, Booking> _bookings;

        public InMemoryBookingRepository()
            : this([])
        {
        }

        public InMemoryBookingRepository(IEnumerable<Booking> bookings)
        {
            _bookings = bookings.ToDictionary(booking => booking.Id);
        }

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
            CancellationToken cancellationToken = default)
        {
            var items = _bookings.Values
                .Where(booking =>
                    booking.Status == BookingStatus.Delivered
                    && booking.DeliveredAtUtc.HasValue
                    && booking.DeliveredAtUtc.Value <= deliveredBeforeUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Booking>>(items);
        }

        public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
        {
            _bookings[booking.Id] = booking;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryCommissionRecordRepository : ICommissionRecordRepository
    {
        public List<CommissionRecord> Items { get; } = [];

        public Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(item => item.BookingId == bookingId));

        public Task AddAsync(CommissionRecord commissionRecord, CancellationToken cancellationToken = default)
        {
            Items.Add(commissionRecord);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryDeliveryRequestRepository : IDeliveryRequestRepository
    {
        private readonly Dictionary<Guid, DeliveryRequest> _requests = [];

        public InMemoryDeliveryRequestRepository(DeliveryRequest? request)
        {
            if (request is not null)
            {
                _requests[request.Id] = request;
            }
        }

        public Task<DeliveryRequest?> FindByIdAsync(Guid deliveryRequestId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.TryGetValue(deliveryRequestId, out var request) ? request : null);

        public Task AddAsync(DeliveryRequest deliveryRequest, CancellationToken cancellationToken = default)
        {
            _requests[deliveryRequest.Id] = deliveryRequest;
            return Task.CompletedTask;
        }

        public Task<bool> HasAssociatedBookingAsync(Guid deliveryRequestId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasBlockingBookingsAsync(Guid deliveryRequestId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<DeliveryRequestListPage> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeliveryRequestListPage([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryTripListingRepository(TripListing trip) : ITripListingRepository
    {
        public Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(tripListingId == trip.Id ? trip : null);

        public Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> HasBlockingBookingsAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<TripSearchPage> SearchAsync(TripSearchCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryTransporterProfileRepository(TransporterProfile profile) : ITransporterProfileRepository
    {
        public Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profileId == profile.Id ? profile : null);

        public Task<TransporterProfile?> FindByIdForUpdateAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profileId == profile.Id ? profile : null);

        public Task<IReadOnlyDictionary<Guid, TransporterProfile>> FindByIdsAsync(
            IEnumerable<Guid> profileIds,
            CancellationToken cancellationToken = default)
        {
            var result = profileIds.Contains(profile.Id)
                ? new Dictionary<Guid, TransporterProfile> { [profile.Id] = profile }
                : new Dictionary<Guid, TransporterProfile>();
            return Task.FromResult<IReadOnlyDictionary<Guid, TransporterProfile>>(result);
        }

        public Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == profile.UserId ? profile : null);

        public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == profile.UserId);

        public Task AddAsync(TransporterProfile profile, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryJobMediator(
        IBookingRepository bookingRepository,
        TimeProvider timeProvider) : IMediator
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is not CompleteBookingCommand command)
            {
                throw new NotSupportedException("Only CompleteBookingCommand is supported in this test mediator.");
            }

            var booking = await bookingRepository.FindByIdAsync(command.BookingId, cancellationToken);
            if (booking is null)
            {
                return (TResponse)(object)Result.Error("BookingNotFound");
            }

            var result = booking.Transition(
                BookingStatus.Completed,
                command.UserId,
                command.ActorRole,
                timeProvider.GetUtcNow().UtcDateTime,
                isSystemAction: command.IsSystemAction);

            if (result.IsSuccess)
            {
                await bookingRepository.SaveChangesAsync(cancellationToken);
            }

            return (TResponse)(object)result;
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<object?>();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Task.CompletedTask;
    }
}
