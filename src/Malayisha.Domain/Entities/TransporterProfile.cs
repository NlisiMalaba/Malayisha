namespace Malayisha.Domain.Entities;

public sealed class TransporterProfile
{
    private TransporterProfile() { }

    private TransporterProfile(
        Guid id,
        Guid userId,
        string displayName,
        IEnumerable<string> routesServed,
        string vehicleDescription,
        decimal capacityKg,
        string? profilePhotoUrl,
        DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        DisplayName = DomainGuard.Required(displayName, nameof(displayName));
        RoutesServed = NormalizeRoutes(routesServed);
        VehicleDescription = DomainGuard.Required(vehicleDescription, nameof(vehicleDescription));
        CapacityKg = DomainGuard.Positive(capacityKg, nameof(capacityKg));
        ProfilePhotoUrl = NormalizeOptionalUrl(profilePhotoUrl);
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public List<string> RoutesServed { get; private set; } = [];
    public string VehicleDescription { get; private set; } = string.Empty;
    public decimal CapacityKg { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public bool IsVerified { get; private set; }
    public decimal AverageRating { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static TransporterProfile Create(
        Guid id,
        Guid userId,
        string displayName,
        IEnumerable<string> routesServed,
        string vehicleDescription,
        decimal capacityKg,
        DateTime nowUtc,
        string? profilePhotoUrl = null) =>
        new(id, userId, displayName, routesServed, vehicleDescription, capacityKg, profilePhotoUrl, nowUtc);

    public void Update(
        string displayName,
        IEnumerable<string> routesServed,
        string vehicleDescription,
        decimal capacityKg,
        DateTime nowUtc)
    {
        DisplayName = DomainGuard.Required(displayName, nameof(displayName));
        RoutesServed = NormalizeRoutes(routesServed);
        VehicleDescription = DomainGuard.Required(vehicleDescription, nameof(vehicleDescription));
        CapacityKg = DomainGuard.Positive(capacityKg, nameof(capacityKg));
        UpdatedAtUtc = nowUtc;
    }

    public void SetProfilePhotoUrl(string profilePhotoUrl, DateTime nowUtc)
    {
        ProfilePhotoUrl = DomainGuard.Required(profilePhotoUrl, nameof(profilePhotoUrl));
        UpdatedAtUtc = nowUtc;
    }

    public void MarkVerified(DateTime nowUtc)
    {
        IsVerified = true;
        UpdatedAtUtc = nowUtc;
    }

    public void SetAverageRating(decimal averageRating, DateTime nowUtc)
    {
        AverageRating = averageRating < 0 ? 0 : averageRating;
        UpdatedAtUtc = nowUtc;
    }

    public void Anonymize(string anonymizedDisplayName, DateTime nowUtc)
    {
        DisplayName = DomainGuard.Required(anonymizedDisplayName, nameof(anonymizedDisplayName));
        ProfilePhotoUrl = null;
        UpdatedAtUtc = nowUtc;
    }

    private static List<string> NormalizeRoutes(IEnumerable<string> routesServed)
    {
        ArgumentNullException.ThrowIfNull(routesServed);

        var normalized = routesServed
            .Select(route => DomainGuard.Required(route, nameof(routesServed)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new ArgumentException("At least one route is required.", nameof(routesServed));
        }

        return normalized;
    }

    private static string? NormalizeOptionalUrl(string? profilePhotoUrl) =>
        string.IsNullOrWhiteSpace(profilePhotoUrl) ? null : profilePhotoUrl.Trim();
}
