using System.Globalization;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.DeliveryRequest;
using Malayisha.Application.Features.DeliveryRequest.CreateDeliveryRequest;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class DeliveryRequestPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool Property21_Create_RoundTripsWritableFields(
        int senderSeed,
        int originSeed,
        int destinationSeed,
        int daysAhead,
        int weightSeed,
        int sizeSeed,
        int goodsSeed)
    {
        return RunCreateRoundTripAsync(
            senderSeed,
            originSeed,
            destinationSeed,
            daysAhead,
            weightSeed,
            sizeSeed,
            goodsSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property15_NonFutureRequiredDate_IsRejected_AndFutureSucceeds(
        int senderSeed,
        int daysOffset)
    {
        return RunFutureDateConstraintAsync(senderSeed, daysOffset).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunCreateRoundTripAsync(
        int senderSeed,
        int originSeed,
        int destinationSeed,
        int daysAhead,
        int weightSeed,
        int sizeSeed,
        int goodsSeed)
    {
        var harness = Harness.Create(senderSeed);
        var payload = BuildValidPayload(
            originSeed,
            destinationSeed,
            NormalizeFutureDays(daysAhead),
            weightSeed,
            sizeSeed,
            goodsSeed);

        var create = await harness.CreateHandler.Handle(
            new CreateDeliveryRequestCommand(
                harness.SenderUserId,
                payload.OriginCity,
                payload.DestinationCity,
                payload.RequiredDateUtc,
                payload.WeightKg,
                payload.SizeDescription,
                payload.GoodsDescription),
            CancellationToken.None);

        if (create.IsError || create.Value is null || create.Value.Id == Guid.Empty)
        {
            return false;
        }

        var created = create.Value;
        var stored = await harness.Requests.FindByIdAsync(created.Id);

        return stored is not null
               && harness.Requests.TotalCount == 1
               && created.SenderId == harness.SenderUserId
               && created.Status == DeliveryRequestStatus.Active
               && FieldsMatch(created, payload)
               && FieldsMatch(DeliveryRequestMappings.ToResponse(stored), payload);
    }

    private static async Task<bool> RunFutureDateConstraintAsync(int senderSeed, int daysOffset)
    {
        var harness = Harness.Create(senderSeed);
        var clampedOffset = daysOffset == int.MinValue
            ? 0
            : (Math.Abs(daysOffset) % 400) * Math.Sign(daysOffset);

        var requiredDate = DateTime.SpecifyKind(BaselineUtc.Date.AddDays(clampedOffset), DateTimeKind.Utc);
        var payload = BuildValidPayload(1, 2, 1, 10, 20, 30) with
        {
            RequiredDateUtc = requiredDate
        };

        var create = await harness.CreateHandler.Handle(
            new CreateDeliveryRequestCommand(
                harness.SenderUserId,
                payload.OriginCity,
                payload.DestinationCity,
                payload.RequiredDateUtc,
                payload.WeightKg,
                payload.SizeDescription,
                payload.GoodsDescription),
            CancellationToken.None);

        if (clampedOffset <= 0)
        {
            return create.IsError
                   && create.ErrorCode == DeliveryRequestErrorCodes.RequiredDateMustBeFuture
                   && create.Value is null
                   && harness.Requests.TotalCount == 0;
        }

        return create.IsSuccess
               && create.Value is not null
               && harness.Requests.TotalCount == 1
               && create.Value.RequiredDateUtc == requiredDate;
    }

    private static bool FieldsMatch(DeliveryRequestResponse actual, DeliveryRequestPayload expected) =>
        actual.OriginCity == expected.OriginCity.Trim()
        && actual.DestinationCity == expected.DestinationCity.Trim()
        && actual.RequiredDateUtc == expected.RequiredDateUtc
        && actual.WeightKg == expected.WeightKg
        && actual.SizeDescription == expected.SizeDescription.Trim()
        && actual.GoodsDescription == expected.GoodsDescription.Trim();

    private static int NormalizeFutureDays(int daysAhead)
    {
        var days = (Math.Abs(daysAhead) % 365) + 1;
        return days;
    }

    private static DeliveryRequestPayload BuildValidPayload(
        int originSeed,
        int destinationSeed,
        int daysAhead,
        int weightSeed,
        int sizeSeed,
        int goodsSeed)
    {
        var origin = BuildToken("Origin", originSeed, DeliveryRequestValidation.CityMaxLength);
        var destination = BuildToken("Dest", destinationSeed, DeliveryRequestValidation.CityMaxLength);
        var requiredDate = DateTime.SpecifyKind(
            BaselineUtc.Date.AddDays(NormalizeFutureDays(daysAhead)),
            DateTimeKind.Utc);
        var weight = (Math.Abs(weightSeed) % 5_000) + 1 + ((Math.Abs(weightSeed) % 100) / 100m);
        var size = BuildToken("Size", sizeSeed, DeliveryRequestValidation.SizeDescriptionMaxLength);
        var goods = BuildToken("Goods", goodsSeed, DeliveryRequestValidation.GoodsDescriptionMaxLength);

        return new DeliveryRequestPayload(origin, destination, requiredDate, weight, size, goods);
    }

    private static string BuildToken(string prefix, int seed, int maxLength)
    {
        var suffix = Math.Abs(seed).ToString(CultureInfo.InvariantCulture);
        var token = $"{prefix}-{suffix}";
        return token.Length <= maxLength ? token : token[..maxLength];
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

    private sealed record DeliveryRequestPayload(
        string OriginCity,
        string DestinationCity,
        DateTime RequiredDateUtc,
        decimal WeightKg,
        string SizeDescription,
        string GoodsDescription);

    private sealed class Harness
    {
        private Harness(
            Guid senderUserId,
            InMemoryDeliveryRequestRepository requests,
            CreateDeliveryRequestCommandHandler createHandler)
        {
            SenderUserId = senderUserId;
            Requests = requests;
            CreateHandler = createHandler;
        }

        public Guid SenderUserId { get; }
        public InMemoryDeliveryRequestRepository Requests { get; }
        public CreateDeliveryRequestCommandHandler CreateHandler { get; }

        public static Harness Create(int senderSeed)
        {
            var senderUserId = BuildUserId(senderSeed);
            var requests = new InMemoryDeliveryRequestRepository();
            var clock = new FixedTimeProvider(BaselineUtc);

            return new Harness(
                senderUserId,
                requests,
                new CreateDeliveryRequestCommandHandler(
                    requests,
                    clock,
                    NullLogger<CreateDeliveryRequestCommandHandler>.Instance));
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InMemoryDeliveryRequestRepository : IDeliveryRequestRepository
    {
        private readonly Dictionary<Guid, Domain.Entities.DeliveryRequest> _byId = [];
        private readonly HashSet<Guid> _associatedRequestIds = [];
        private readonly HashSet<Guid> _blockingRequestIds = [];

        public int TotalCount => _byId.Count;

        public Task<Domain.Entities.DeliveryRequest?> FindByIdAsync(
            Guid deliveryRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(deliveryRequestId, out var request) ? request : null);

        public Task AddAsync(
            Domain.Entities.DeliveryRequest deliveryRequest,
            CancellationToken cancellationToken = default)
        {
            _byId[deliveryRequest.Id] = deliveryRequest;
            return Task.CompletedTask;
        }

        public Task<bool> HasAssociatedBookingAsync(
            Guid deliveryRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_associatedRequestIds.Contains(deliveryRequestId));

        public Task<bool> HasBlockingBookingsAsync(
            Guid deliveryRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_blockingRequestIds.Contains(deliveryRequestId));

        public Task<DeliveryRequestListPage> ListActiveAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var active = _byId.Values
                .Where(request => request.Status == DeliveryRequestStatus.Active)
                .OrderBy(request => request.RequiredDateUtc)
                .ThenBy(request => request.Id)
                .ToArray();

            var items = active
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(new DeliveryRequestListPage(items, active.Length));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
