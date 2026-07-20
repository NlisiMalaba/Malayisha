using System.Globalization;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Review;
using Malayisha.Application.Features.Review.CreateReview;
using Malayisha.Application.Features.Review.GetAllReviews;
using Malayisha.Application.Features.Review.GetTransporterReviews;
using Malayisha.Application.Features.Review.HideReview;
using Malayisha.Application.Features.Review.RestoreReview;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class ReviewPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    private static readonly BookingStatus[] NonCompletedStatuses =
    [
        BookingStatus.Requested,
        BookingStatus.Quoted,
        BookingStatus.Confirmed,
        BookingStatus.InTransit,
        BookingStatus.Delivered,
        BookingStatus.Cancelled
    ];

    [Property(MaxTest = 100)]
    public bool Property27_ReviewRoundTripAndAverageRatingInvariant(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        int reviewCountSeed,
        int ratingSeed,
        int commentSeed)
    {
        return RunReviewRoundTripAsync(
            senderSeed,
            transporterSeed,
            tripSeed,
            reviewCountSeed,
            ratingSeed,
            commentSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property28_OneReviewPerBookingInvariant(
        int senderSeed,
        int transporterSeed,
        int bookingSeed,
        int ratingSeed)
    {
        return RunOneReviewPerBookingAsync(
            senderSeed,
            transporterSeed,
            bookingSeed,
            ratingSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property29_ReviewStatusGuard_RejectsNonCompletedBookings(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        int statusSeed,
        int ratingSeed)
    {
        return RunReviewStatusGuardAsync(
            senderSeed,
            transporterSeed,
            tripSeed,
            statusSeed,
            ratingSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property30_HiddenReviewExcludedFromPublicListings_HideRestoreCycle(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        int reviewCountSeed,
        int ratingSeed,
        int hideIndexSeed,
        int adminSeed)
    {
        return RunHiddenReviewExcludedAsync(
            senderSeed,
            transporterSeed,
            tripSeed,
            reviewCountSeed,
            ratingSeed,
            hideIndexSeed,
            adminSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunReviewRoundTripAsync(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        int reviewCountSeed,
        int ratingSeed,
        int commentSeed)
    {
        var harness = ReviewTestHarness.Create(senderSeed, transporterSeed, tripSeed);
        var reviewCount = (Math.Abs(reviewCountSeed) % 5) + 1;
        var ratings = new List<int>();
        var createdReviews = new List<ReviewDto>();

        for (var index = 0; index < reviewCount; index++)
        {
            var bookingId = BuildGuid(tripSeed + index + 1);
            var booking = CreateCompletedBooking(
                bookingId,
                harness.TripId,
                harness.SenderId,
                harness.TransporterId,
                BaselineUtc.AddHours(index));

            await harness.Bookings.AddAsync(booking);

            var rating = (Math.Abs(ratingSeed + index) % ReviewConstants.MaxRating) + ReviewConstants.MinRating;
            var comment = BuildComment(commentSeed + index);

            var create = await harness.CreateHandler.Handle(
                new CreateReviewCommand(harness.SenderId, bookingId, rating, comment),
                CancellationToken.None);

            if (create.IsError || create.Value is null)
            {
                return false;
            }

            ratings.Add(rating);
            createdReviews.Add(create.Value);

            var expectedAverage = ReviewAverageCalculator.Calculate(ratings);
            var profile = await harness.Profiles.FindByIdAsync(harness.ProfileId);
            if (profile is null || profile.AverageRating != expectedAverage)
            {
                return false;
            }

            if (create.Value.BookingId != bookingId
                || create.Value.SenderId != harness.SenderId
                || create.Value.Rating != rating
                || create.Value.Comment != comment
                || create.Value.CreatedAtUtc == default)
            {
                return false;
            }
        }

        var listed = await harness.ListHandler.Handle(
            new GetTransporterReviewsQuery(harness.ProfileId),
            CancellationToken.None);

        if (listed.IsError || listed.Value is null || listed.Value.Count != reviewCount)
        {
            return false;
        }

        for (var index = 1; index < listed.Value.Count; index++)
        {
            if (listed.Value[index].CreatedAtUtc > listed.Value[index - 1].CreatedAtUtc)
            {
                return false;
            }
        }

        var expectedByBooking = createdReviews.ToDictionary(review => review.BookingId);
        foreach (var review in listed.Value)
        {
            if (!expectedByBooking.TryGetValue(review.BookingId, out var expected))
            {
                return false;
            }

            if (review.Id != expected.Id
                || review.SenderId != expected.SenderId
                || review.Rating != expected.Rating
                || review.Comment != expected.Comment
                || review.CreatedAtUtc != expected.CreatedAtUtc)
            {
                return false;
            }
        }

        return harness.Reviews.Items.Count == reviewCount;
    }

    private static async Task<bool> RunHiddenReviewExcludedAsync(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        int reviewCountSeed,
        int ratingSeed,
        int hideIndexSeed,
        int adminSeed)
    {
        var harness = ReviewTestHarness.Create(senderSeed, transporterSeed, tripSeed);
        var reviewCount = (Math.Abs(reviewCountSeed) % 5) + 2;
        var ratings = new List<int>();
        var createdReviews = new List<ReviewDto>();

        for (var index = 0; index < reviewCount; index++)
        {
            var bookingId = BuildGuid(tripSeed + index + 1);
            var booking = CreateCompletedBooking(
                bookingId,
                harness.TripId,
                harness.SenderId,
                harness.TransporterId,
                BaselineUtc.AddHours(index));

            await harness.Bookings.AddAsync(booking);

            var rating = (Math.Abs(ratingSeed + index) % ReviewConstants.MaxRating) + ReviewConstants.MinRating;
            var create = await harness.CreateHandler.Handle(
                new CreateReviewCommand(harness.SenderId, bookingId, rating, null),
                CancellationToken.None);

            if (create.IsError || create.Value is null)
            {
                return false;
            }

            ratings.Add(rating);
            createdReviews.Add(create.Value);
        }

        var hideIndex = Math.Abs(hideIndexSeed) % reviewCount;
        var reviewToHide = createdReviews[hideIndex];
        var adminUserId = BuildUserId(adminSeed ^ 0x33333333);

        var hide = await harness.HideHandler.Handle(
            new HideReviewCommand(reviewToHide.Id, adminUserId),
            CancellationToken.None);

        if (hide.IsError || hide.Value is null || !hide.Value.IsHidden)
        {
            return false;
        }

        var publicAfterHide = await harness.ListHandler.Handle(
            new GetTransporterReviewsQuery(harness.ProfileId),
            CancellationToken.None);

        if (publicAfterHide.IsError
            || publicAfterHide.Value is null
            || publicAfterHide.Value.Count != reviewCount - 1
            || publicAfterHide.Value.Any(review => review.Id == reviewToHide.Id))
        {
            return false;
        }

        var adminAfterHide = await harness.GetAllHandler.Handle(
            new GetAllReviewsQuery(),
            CancellationToken.None);

        if (adminAfterHide.IsError
            || adminAfterHide.Value is null
            || adminAfterHide.Value.Count != reviewCount)
        {
            return false;
        }

        var hiddenInAdmin = adminAfterHide.Value.First(review => review.Id == reviewToHide.Id);
        if (!hiddenInAdmin.IsHidden)
        {
            return false;
        }

        var visibleRatings = ratings
            .Where((_, index) => index != hideIndex)
            .ToList();
        var profile = await harness.Profiles.FindByIdAsync(harness.ProfileId);
        if (profile is null
            || profile.AverageRating != ReviewAverageCalculator.Calculate(visibleRatings))
        {
            return false;
        }

        var hideAudit = harness.AuditLogs.SingleOrDefault(
            log => log.TargetId == reviewToHide.Id
                   && log.Action == ReviewAuditActions.Hidden);

        if (hideAudit is null
            || hideAudit.ActorUserId != adminUserId
            || hideAudit.TargetType != ReviewAuditActions.TargetType)
        {
            return false;
        }

        var restore = await harness.RestoreHandler.Handle(
            new RestoreReviewCommand(reviewToHide.Id, adminUserId),
            CancellationToken.None);

        if (restore.IsError || restore.Value is null || restore.Value.IsHidden)
        {
            return false;
        }

        var publicAfterRestore = await harness.ListHandler.Handle(
            new GetTransporterReviewsQuery(harness.ProfileId),
            CancellationToken.None);

        if (publicAfterRestore.IsError
            || publicAfterRestore.Value is null
            || publicAfterRestore.Value.Count != reviewCount
            || !publicAfterRestore.Value.Any(review => review.Id == reviewToHide.Id))
        {
            return false;
        }

        var adminAfterRestore = await harness.GetAllHandler.Handle(
            new GetAllReviewsQuery(),
            CancellationToken.None);

        if (adminAfterRestore.IsError || adminAfterRestore.Value is null)
        {
            return false;
        }

        var restoredInAdmin = adminAfterRestore.Value.First(review => review.Id == reviewToHide.Id);
        if (restoredInAdmin.IsHidden)
        {
            return false;
        }

        profile = await harness.Profiles.FindByIdAsync(harness.ProfileId);
        if (profile is null
            || profile.AverageRating != ReviewAverageCalculator.Calculate(ratings))
        {
            return false;
        }

        var restoreAudit = harness.AuditLogs.SingleOrDefault(
            log => log.TargetId == reviewToHide.Id
                   && log.Action == ReviewAuditActions.Restored);

        return restoreAudit is not null
               && restoreAudit.ActorUserId == adminUserId
               && restoreAudit.TargetType == ReviewAuditActions.TargetType;
    }

    private static async Task<bool> RunOneReviewPerBookingAsync(
        int senderSeed,
        int transporterSeed,
        int bookingSeed,
        int ratingSeed)
    {
        var harness = ReviewTestHarness.Create(senderSeed, transporterSeed, bookingSeed);
        var bookingId = BuildGuid(bookingSeed);
        var booking = CreateCompletedBooking(
            bookingId,
            harness.TripId,
            harness.SenderId,
            harness.TransporterId,
            BaselineUtc);

        await harness.Bookings.AddAsync(booking);

        var rating = (Math.Abs(ratingSeed) % ReviewConstants.MaxRating) + ReviewConstants.MinRating;

        var first = await harness.CreateHandler.Handle(
            new CreateReviewCommand(harness.SenderId, bookingId, rating, "first"),
            CancellationToken.None);

        if (first.IsError || first.Value is null)
        {
            return false;
        }

        var duplicate = await harness.CreateHandler.Handle(
            new CreateReviewCommand(harness.SenderId, bookingId, rating, "duplicate"),
            CancellationToken.None);

        return duplicate.IsError
               && duplicate.ErrorCode == ReviewErrorCodes.ReviewAlreadyExists
               && duplicate.Value is null
               && harness.Reviews.Items.Count == 1;
    }

    private static async Task<bool> RunReviewStatusGuardAsync(
        int senderSeed,
        int transporterSeed,
        int tripSeed,
        int statusSeed,
        int ratingSeed)
    {
        var harness = ReviewTestHarness.Create(senderSeed, transporterSeed, tripSeed);
        var status = NonCompletedStatuses[Math.Abs(statusSeed) % NonCompletedStatuses.Length];
        var bookingId = BuildGuid(tripSeed ^ unchecked((int)0xABCDEF01));
        var booking = CreateBookingAtStatus(
            bookingId,
            harness.TripId,
            harness.SenderId,
            harness.TransporterId,
            status,
            BaselineUtc);

        await harness.Bookings.AddAsync(booking);

        var rating = (Math.Abs(ratingSeed) % ReviewConstants.MaxRating) + ReviewConstants.MinRating;

        var create = await harness.CreateHandler.Handle(
            new CreateReviewCommand(harness.SenderId, bookingId, rating, null),
            CancellationToken.None);

        return create.IsError
               && create.ErrorCode == ReviewErrorCodes.BookingNotCompleted
               && create.Value is null
               && harness.Reviews.Items.Count == 0;
    }

    private static Booking CreateCompletedBooking(
        Guid bookingId,
        Guid tripListingId,
        Guid senderId,
        Guid transporterId,
        DateTime baselineUtc) =>
        CreateBookingAtStatus(
            bookingId,
            tripListingId,
            senderId,
            transporterId,
            BookingStatus.Completed,
            baselineUtc);

    private static Booking CreateBookingAtStatus(
        Guid bookingId,
        Guid tripListingId,
        Guid senderId,
        Guid transporterId,
        BookingStatus status,
        DateTime baselineUtc)
    {
        var booking = Booking.Create(
            bookingId,
            tripListingId,
            senderId,
            transporterId,
            baselineUtc,
            "review-test");

        var stepTime = baselineUtc;

        switch (status)
        {
            case BookingStatus.Requested:
                break;
            case BookingStatus.Quoted:
                _ = booking.Transition(
                    BookingStatus.Quoted,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(1),
                    700m);
                break;
            case BookingStatus.Confirmed:
                _ = booking.Transition(
                    BookingStatus.Quoted,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(1),
                    700m);
                _ = booking.Transition(
                    BookingStatus.Confirmed,
                    senderId,
                    UserRole.Sender,
                    stepTime = stepTime.AddMinutes(2),
                    750m);
                break;
            case BookingStatus.InTransit:
                _ = booking.Transition(
                    BookingStatus.Quoted,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(1),
                    700m);
                _ = booking.Transition(
                    BookingStatus.Confirmed,
                    senderId,
                    UserRole.Sender,
                    stepTime = stepTime.AddMinutes(2),
                    750m);
                _ = booking.Transition(
                    BookingStatus.InTransit,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(3));
                break;
            case BookingStatus.Delivered:
                _ = booking.Transition(
                    BookingStatus.Quoted,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(1),
                    700m);
                _ = booking.Transition(
                    BookingStatus.Confirmed,
                    senderId,
                    UserRole.Sender,
                    stepTime = stepTime.AddMinutes(2),
                    750m);
                _ = booking.Transition(
                    BookingStatus.InTransit,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(3));
                _ = booking.Transition(
                    BookingStatus.Delivered,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(4));
                break;
            case BookingStatus.Completed:
                _ = booking.Transition(
                    BookingStatus.Quoted,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(1),
                    700m);
                _ = booking.Transition(
                    BookingStatus.Confirmed,
                    senderId,
                    UserRole.Sender,
                    stepTime = stepTime.AddMinutes(2),
                    750m);
                _ = booking.Transition(
                    BookingStatus.InTransit,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(3));
                _ = booking.Transition(
                    BookingStatus.Delivered,
                    transporterId,
                    UserRole.Transporter,
                    stepTime = stepTime.AddMinutes(4));
                _ = booking.Transition(
                    BookingStatus.Completed,
                    senderId,
                    UserRole.Sender,
                    stepTime = stepTime.AddMinutes(5));
                break;
            case BookingStatus.Cancelled:
                _ = booking.Transition(
                    BookingStatus.Cancelled,
                    senderId,
                    UserRole.Sender,
                    stepTime = stepTime.AddMinutes(1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return booking;
    }

    private static string? BuildComment(int seed)
    {
        if (Math.Abs(seed) % 3 == 0)
        {
            return null;
        }

        var length = (Math.Abs(seed) % 120) + 1;
        var prefix = $"review-{Math.Abs(seed).ToString(CultureInfo.InvariantCulture)}-";
        return prefix.Length >= length
            ? prefix[..length]
            : prefix + new string('x', length - prefix.Length);
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

    private static Guid BuildUserId(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private sealed class ReviewTestHarness
    {
        private ReviewTestHarness(
            Guid senderId,
            Guid transporterId,
            Guid tripId,
            Guid profileId,
            InMemoryBookingRepository bookings,
            InMemoryTransporterProfileRepository profiles,
            InMemoryReviewRepository reviews,
            InMemoryAuditLogRepository auditLogs,
            CreateReviewCommandHandler createHandler,
            GetTransporterReviewsQueryHandler listHandler,
            HideReviewCommandHandler hideHandler,
            RestoreReviewCommandHandler restoreHandler,
            GetAllReviewsQueryHandler getAllHandler)
        {
            SenderId = senderId;
            TransporterId = transporterId;
            TripId = tripId;
            ProfileId = profileId;
            Bookings = bookings;
            Profiles = profiles;
            Reviews = reviews;
            AuditLogs = auditLogs.Items;
            CreateHandler = createHandler;
            ListHandler = listHandler;
            HideHandler = hideHandler;
            RestoreHandler = restoreHandler;
            GetAllHandler = getAllHandler;
        }

        public Guid SenderId { get; }
        public Guid TransporterId { get; }
        public Guid TripId { get; }
        public Guid ProfileId { get; }
        public InMemoryBookingRepository Bookings { get; }
        public InMemoryTransporterProfileRepository Profiles { get; }
        public InMemoryReviewRepository Reviews { get; }
        public IReadOnlyList<AuditLog> AuditLogs { get; }
        public CreateReviewCommandHandler CreateHandler { get; }
        public GetTransporterReviewsQueryHandler ListHandler { get; }
        public HideReviewCommandHandler HideHandler { get; }
        public RestoreReviewCommandHandler RestoreHandler { get; }
        public GetAllReviewsQueryHandler GetAllHandler { get; }

        public static ReviewTestHarness Create(int senderSeed, int transporterSeed, int tripSeed)
        {
            var senderId = BuildUserId(senderSeed);
            var transporterId = BuildUserId(transporterSeed);
            if (transporterId == senderId)
            {
                transporterId = BuildUserId(transporterSeed ^ 0x13579BDF);
            }

            var tripId = BuildGuid(tripSeed);
            var profileId = BuildGuid(tripSeed ^ 0x2468ACE0);
            var clock = new IncrementingTimeProvider(BaselineUtc);

            var profile = TransporterProfile.Create(
                profileId,
                transporterId,
                "Transporter",
                ["JHB-HRE"],
                "Van",
                500m,
                BaselineUtc);

            var trip = TripListing.Create(
                tripId,
                profileId,
                "Johannesburg",
                "Harare",
                BaselineUtc.AddDays(5),
                200m,
                600m,
                BaselineUtc);

            var bookings = new InMemoryBookingRepository();
            var profiles = new InMemoryTransporterProfileRepository(profile);
            var trips = new InMemoryTripListingRepository(trip);
            var reviews = new InMemoryReviewRepository();
            var auditLogs = new InMemoryAuditLogRepository();

            return new ReviewTestHarness(
                senderId,
                transporterId,
                tripId,
                profileId,
                bookings,
                profiles,
                reviews,
                auditLogs,
                new CreateReviewCommandHandler(
                    bookings,
                    trips,
                    profiles,
                    reviews,
                    clock,
                    NullLogger<CreateReviewCommandHandler>.Instance),
                new GetTransporterReviewsQueryHandler(
                    profiles,
                    reviews,
                    NullLogger<GetTransporterReviewsQueryHandler>.Instance),
                new HideReviewCommandHandler(
                    reviews,
                    profiles,
                    auditLogs,
                    clock,
                    NullLogger<HideReviewCommandHandler>.Instance),
                new RestoreReviewCommandHandler(
                    reviews,
                    profiles,
                    auditLogs,
                    clock,
                    NullLogger<RestoreReviewCommandHandler>.Instance),
                new GetAllReviewsQueryHandler(
                    reviews,
                    NullLogger<GetAllReviewsQueryHandler>.Instance));
        }
    }

    private sealed class InMemoryAuditLogRepository : IAuditLogRepository
    {
        private readonly List<AuditLog> _items = [];

        public IReadOnlyList<AuditLog> Items => _items;

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            _items.Add(auditLog);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBookingRepository : IBookingRepository
    {
        private readonly Dictionary<Guid, Booking> _bookings = [];

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

    private sealed class InMemoryTripListingRepository(TripListing trip) : ITripListingRepository
    {
        public Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(tripListingId == trip.Id ? trip : null);

        public Task<TripListing?> FindByIdForUpdateAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(tripListingId, cancellationToken);

        public Task<IReadOnlyList<TripListing>> ListExpiredBoostedForUpdateAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TripListing>>(
                trip.IsBoosted
                && trip.BoostEndAtUtc != null
                && trip.BoostEndAtUtc <= nowUtc
                    ? new[] { trip }
                    : []);

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

        public Task<TransporterProfile?> FindByIdForUpdateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
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

    private sealed class InMemoryReviewRepository : IReviewRepository
    {
        private readonly List<Review> _reviews = [];

        public IReadOnlyList<Review> Items => _reviews;

        public Task<bool> ExistsByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_reviews.Any(review => review.BookingId == bookingId));

        public Task<IReadOnlyList<int>> ListPublicRatingsByTransporterProfileIdAsync(
            Guid transporterProfileId,
            CancellationToken cancellationToken = default)
        {
            var ratings = _reviews
                .Where(review => review.TransporterProfileId == transporterProfileId && !review.IsHidden)
                .Select(review => review.Rating)
                .ToArray();

            return Task.FromResult<IReadOnlyList<int>>(ratings);
        }

        public Task<IReadOnlyList<Review>> ListPublicByTransporterProfileIdAsync(
            Guid transporterProfileId,
            CancellationToken cancellationToken = default)
        {
            var items = _reviews
                .Where(review => review.TransporterProfileId == transporterProfileId && !review.IsHidden)
                .OrderByDescending(review => review.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Review>>(items);
        }

        public Task<Review?> FindByIdForUpdateAsync(Guid reviewId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_reviews.FirstOrDefault(review => review.Id == reviewId));

        public Task<IReadOnlyList<Review>> ListAllOrderedByCreatedAtDescAsync(
            CancellationToken cancellationToken = default)
        {
            var items = _reviews
                .OrderByDescending(review => review.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<Review>>(items);
        }

        public Task AddAsync(Review review, CancellationToken cancellationToken = default)
        {
            _reviews.Add(review);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class IncrementingTimeProvider : TimeProvider
    {
        private DateTimeOffset _current;

        public IncrementingTimeProvider(DateTime utcStart) =>
            _current = new DateTimeOffset(utcStart, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            var result = _current;
            _current = _current.AddMinutes(1);
            return result;
        }
    }
}
