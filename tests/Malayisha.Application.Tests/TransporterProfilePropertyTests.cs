using System.Reflection;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Profile;
using Malayisha.Application.Features.Profile.CreateProfile;
using Malayisha.Application.Features.Profile.GetPublicProfile;
using Malayisha.Application.Features.Profile.UpdateProfile;
using Malayisha.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class TransporterProfilePropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly string[] PublicResponsePropertyNames =
    [
        nameof(PublicTransporterProfileResponse.Id),
        nameof(PublicTransporterProfileResponse.DisplayName),
        nameof(PublicTransporterProfileResponse.RoutesServed),
        nameof(PublicTransporterProfileResponse.VehicleDescription),
        nameof(PublicTransporterProfileResponse.CapacityKg),
        nameof(PublicTransporterProfileResponse.ProfilePhotoUrl),
        nameof(PublicTransporterProfileResponse.IsVerified),
        nameof(PublicTransporterProfileResponse.AverageRating)
    ];

    [Property(MaxTest = 100)]
    public bool Property6_CreateThenGet_RoundTripsWritableFields(
        int userSeed,
        int nameSeed,
        int routeSeed,
        int vehicleSeed,
        int capacitySeed,
        int photoSeed)
    {
        return RunCreateGetRoundTripAsync(
            userSeed,
            nameSeed,
            routeSeed,
            vehicleSeed,
            capacitySeed,
            photoSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property6_UpdateThenGet_RoundTripsWritableFields(
        int userSeed,
        int createSeed,
        int updateSeed)
    {
        return RunUpdateGetRoundTripAsync(userSeed, createSeed, updateSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property7_SecondCreate_IsRejected_AndDoesNotCreateSecondProfile(int userSeed, int payloadSeed)
    {
        return RunOneProfileInvariantAsync(userSeed, payloadSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property8_PublicProfile_ContainsOnlyPublicFields(int userSeed, int payloadSeed)
    {
        return RunPublicFieldsAsync(userSeed, payloadSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunCreateGetRoundTripAsync(
        int userSeed,
        int nameSeed,
        int routeSeed,
        int vehicleSeed,
        int capacitySeed,
        int photoSeed)
    {
        var userId = BuildUserId(userSeed);
        var payload = BuildValidPayload(nameSeed, routeSeed, vehicleSeed, capacitySeed, photoSeed);
        var repository = new InMemoryTransporterProfileRepository();
        var createHandler = CreateCreateHandler(repository);
        var getHandler = new GetPublicProfileQueryHandler(repository);

        var createResult = await createHandler.Handle(
            new CreateProfileCommand(
                userId,
                payload.DisplayName,
                payload.RoutesServed,
                payload.VehicleDescription,
                payload.CapacityKg,
                payload.ProfilePhotoUrl),
            CancellationToken.None);

        if (createResult.IsError || createResult.Value is null)
        {
            return false;
        }

        var created = createResult.Value;
        if (!WritableFieldsMatch(created.DisplayName, created.RoutesServed, created.VehicleDescription,
                created.CapacityKg, created.ProfilePhotoUrl, payload))
        {
            return false;
        }

        var getResult = await getHandler.Handle(new GetPublicProfileQuery(created.Id), CancellationToken.None);
        if (getResult.IsError || getResult.Value is null)
        {
            return false;
        }

        var publicProfile = getResult.Value;
        return publicProfile.Id == created.Id
               && WritableFieldsMatch(
                   publicProfile.DisplayName,
                   publicProfile.RoutesServed,
                   publicProfile.VehicleDescription,
                   publicProfile.CapacityKg,
                   publicProfile.ProfilePhotoUrl,
                   payload);
    }

    private static async Task<bool> RunUpdateGetRoundTripAsync(int userSeed, int createSeed, int updateSeed)
    {
        var userId = BuildUserId(userSeed);
        var createPayload = BuildValidPayload(createSeed, createSeed + 11, createSeed + 19, createSeed + 29, createSeed + 37);
        var updatePayload = BuildValidPayload(updateSeed, updateSeed + 13, updateSeed + 23, updateSeed + 31, updateSeed + 41);

        var repository = new InMemoryTransporterProfileRepository();
        var createHandler = CreateCreateHandler(repository);
        var updateHandler = CreateUpdateHandler(repository);
        var getHandler = new GetPublicProfileQueryHandler(repository);

        var createResult = await createHandler.Handle(
            new CreateProfileCommand(
                userId,
                createPayload.DisplayName,
                createPayload.RoutesServed,
                createPayload.VehicleDescription,
                createPayload.CapacityKg,
                createPayload.ProfilePhotoUrl),
            CancellationToken.None);

        if (createResult.IsError || createResult.Value is null)
        {
            return false;
        }

        var updateResult = await updateHandler.Handle(
            new UpdateProfileCommand(
                userId,
                updatePayload.DisplayName,
                updatePayload.RoutesServed,
                updatePayload.VehicleDescription,
                updatePayload.CapacityKg),
            CancellationToken.None);

        if (updateResult.IsError || updateResult.Value is null)
        {
            return false;
        }

        var updated = updateResult.Value;
        if (updated.DisplayName != updatePayload.DisplayName.Trim()
            || !RoutesEqual(updated.RoutesServed, updatePayload.RoutesServed)
            || updated.VehicleDescription != updatePayload.VehicleDescription.Trim()
            || updated.CapacityKg != updatePayload.CapacityKg
            || updated.ProfilePhotoUrl != createPayload.ProfilePhotoUrl)
        {
            return false;
        }

        var getResult = await getHandler.Handle(new GetPublicProfileQuery(updated.Id), CancellationToken.None);
        if (getResult.IsError || getResult.Value is null)
        {
            return false;
        }

        var publicProfile = getResult.Value;
        return publicProfile.Id == updated.Id
               && publicProfile.DisplayName == updatePayload.DisplayName.Trim()
               && RoutesEqual(publicProfile.RoutesServed, updatePayload.RoutesServed)
               && publicProfile.VehicleDescription == updatePayload.VehicleDescription.Trim()
               && publicProfile.CapacityKg == updatePayload.CapacityKg
               && publicProfile.ProfilePhotoUrl == createPayload.ProfilePhotoUrl;
    }

    private static async Task<bool> RunOneProfileInvariantAsync(int userSeed, int payloadSeed)
    {
        var userId = BuildUserId(userSeed);
        var firstPayload = BuildValidPayload(payloadSeed, payloadSeed + 3, payloadSeed + 7, payloadSeed + 11, payloadSeed + 13);
        var secondPayload = BuildValidPayload(payloadSeed + 17, payloadSeed + 19, payloadSeed + 23, payloadSeed + 29, payloadSeed + 31);

        var repository = new InMemoryTransporterProfileRepository();
        var createHandler = CreateCreateHandler(repository);

        var firstResult = await createHandler.Handle(
            new CreateProfileCommand(
                userId,
                firstPayload.DisplayName,
                firstPayload.RoutesServed,
                firstPayload.VehicleDescription,
                firstPayload.CapacityKg,
                firstPayload.ProfilePhotoUrl),
            CancellationToken.None);

        if (firstResult.IsError || firstResult.Value is null)
        {
            return false;
        }

        var secondResult = await createHandler.Handle(
            new CreateProfileCommand(
                userId,
                secondPayload.DisplayName,
                secondPayload.RoutesServed,
                secondPayload.VehicleDescription,
                secondPayload.CapacityKg,
                secondPayload.ProfilePhotoUrl),
            CancellationToken.None);

        return secondResult.IsError
               && secondResult.ErrorCode == ProfileErrorCodes.ProfileAlreadyExists
               && secondResult.Value is null
               && repository.CountForUser(userId) == 1
               && repository.TotalCount == 1;
    }

    private static async Task<bool> RunPublicFieldsAsync(int userSeed, int payloadSeed)
    {
        var userId = BuildUserId(userSeed);
        var payload = BuildValidPayload(payloadSeed, payloadSeed + 5, payloadSeed + 9, payloadSeed + 15, payloadSeed + 21);
        var repository = new InMemoryTransporterProfileRepository();
        var createHandler = CreateCreateHandler(repository);
        var getHandler = new GetPublicProfileQueryHandler(repository);

        var createResult = await createHandler.Handle(
            new CreateProfileCommand(
                userId,
                payload.DisplayName,
                payload.RoutesServed,
                payload.VehicleDescription,
                payload.CapacityKg,
                payload.ProfilePhotoUrl),
            CancellationToken.None);

        if (createResult.IsError || createResult.Value is null)
        {
            return false;
        }

        var getResult = await getHandler.Handle(
            new GetPublicProfileQuery(createResult.Value.Id),
            CancellationToken.None);

        if (getResult.IsError || getResult.Value is null)
        {
            return false;
        }

        var publicProfile = getResult.Value;
        var propertyNames = typeof(PublicTransporterProfileResponse)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var expectedPropertyNames = PublicResponsePropertyNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (!propertyNames.SequenceEqual(expectedPropertyNames, StringComparer.Ordinal))
        {
            return false;
        }

        if (propertyNames.Contains(nameof(TransporterProfileResponse.UserId), StringComparer.Ordinal)
            || propertyNames.Contains(nameof(TransporterProfileResponse.CreatedAtUtc), StringComparer.Ordinal)
            || propertyNames.Contains(nameof(TransporterProfileResponse.UpdatedAtUtc), StringComparer.Ordinal))
        {
            return false;
        }

        return publicProfile.Id == createResult.Value.Id
               && publicProfile.DisplayName == payload.DisplayName.Trim()
               && RoutesEqual(publicProfile.RoutesServed, payload.RoutesServed)
               && publicProfile.VehicleDescription == payload.VehicleDescription.Trim()
               && publicProfile.CapacityKg == payload.CapacityKg
               && publicProfile.ProfilePhotoUrl == payload.ProfilePhotoUrl
               && publicProfile.IsVerified == false
               && publicProfile.AverageRating == 0m;
    }

    private static CreateProfileCommandHandler CreateCreateHandler(InMemoryTransporterProfileRepository repository) =>
        new(repository, new FixedTimeProvider(BaselineUtc), NullLogger<CreateProfileCommandHandler>.Instance);

    private static UpdateProfileCommandHandler CreateUpdateHandler(InMemoryTransporterProfileRepository repository) =>
        new(repository, new FixedTimeProvider(BaselineUtc), NullLogger<UpdateProfileCommandHandler>.Instance);

    private static bool WritableFieldsMatch(
        string displayName,
        IReadOnlyList<string> routesServed,
        string vehicleDescription,
        decimal capacityKg,
        string? profilePhotoUrl,
        ProfilePayload expected) =>
        displayName == expected.DisplayName.Trim()
        && RoutesEqual(routesServed, expected.RoutesServed)
        && vehicleDescription == expected.VehicleDescription.Trim()
        && capacityKg == expected.CapacityKg
        && profilePhotoUrl == expected.ProfilePhotoUrl;

    private static bool RoutesEqual(IReadOnlyList<string> actual, IReadOnlyList<string> expected) =>
        actual.Count == expected.Count
        && actual.SequenceEqual(expected, StringComparer.Ordinal);

    private static Guid BuildUserId(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private static ProfilePayload BuildValidPayload(
        int nameSeed,
        int routeSeed,
        int vehicleSeed,
        int capacitySeed,
        int photoSeed)
    {
        var displayName = BuildToken("Driver", nameSeed, ProfileValidation.DisplayNameMaxLength);
        var vehicleDescription = BuildToken("Vehicle", vehicleSeed, ProfileValidation.VehicleDescriptionMaxLength);
        var routeCount = (Math.Abs(routeSeed) % 5) + 1;
        var routes = Enumerable.Range(0, routeCount)
            .Select(index => BuildToken($"Route-{index}", routeSeed + index, ProfileValidation.RouteMaxLength))
            .ToArray();

        var capacityKg = (Math.Abs(capacitySeed) % 5_000) + 1 + ((Math.Abs(capacitySeed) % 100) / 100m);
        var profilePhotoUrl = photoSeed % 2 == 0
            ? null
            : $"https://cdn.example.test/profile/{Math.Abs(photoSeed)}.jpg";

        return new ProfilePayload(displayName, routes, vehicleDescription, capacityKg, profilePhotoUrl);
    }

    private static string BuildToken(string prefix, int seed, int maxLength)
    {
        var value = Math.Abs(seed);
        var suffix = value.ToString();
        var token = $"{prefix}-{suffix}";
        return token.Length <= maxLength ? token : token[..maxLength];
    }

    private sealed record ProfilePayload(
        string DisplayName,
        IReadOnlyList<string> RoutesServed,
        string VehicleDescription,
        decimal CapacityKg,
        string? ProfilePhotoUrl);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InMemoryTransporterProfileRepository : ITransporterProfileRepository
    {
        private readonly Dictionary<Guid, TransporterProfile> _byId = [];
        private readonly Dictionary<Guid, Guid> _idByUserId = [];

        public int TotalCount => _byId.Count;

        public int CountForUser(Guid userId) => _idByUserId.ContainsKey(userId) ? 1 : 0;

        public Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(profileId, out var profile) ? profile : null);

        public Task<TransporterProfile?> FindByIdForUpdateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(profileId, cancellationToken);

        public Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (!_idByUserId.TryGetValue(userId, out var profileId))
            {
                return Task.FromResult<TransporterProfile?>(null);
            }

            return Task.FromResult<TransporterProfile?>(_byId[profileId]);
        }

        public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_idByUserId.ContainsKey(userId));

        public Task AddAsync(TransporterProfile profile, CancellationToken cancellationToken = default)
        {
            _byId[profile.Id] = profile;
            _idByUserId[profile.UserId] = profile.Id;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
