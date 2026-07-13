namespace Malayisha.Domain.Entities;

public sealed class TransporterProfile
{
    private TransporterProfile() { }

    private TransporterProfile(
        Guid id,
        Guid userId,
        string displayName,
        string vehicleDescription,
        decimal capacityKg,
        DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        DisplayName = DomainGuard.Required(displayName, nameof(displayName));
        VehicleDescription = DomainGuard.Required(vehicleDescription, nameof(vehicleDescription));
        CapacityKg = DomainGuard.Positive(capacityKg, nameof(capacityKg));
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
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
        string vehicleDescription,
        decimal capacityKg,
        DateTime nowUtc) =>
        new(id, userId, displayName, vehicleDescription, capacityKg, nowUtc);

    public void Update(
        string displayName,
        string vehicleDescription,
        decimal capacityKg,
        string? profilePhotoUrl,
        DateTime nowUtc)
    {
        DisplayName = DomainGuard.Required(displayName, nameof(displayName));
        VehicleDescription = DomainGuard.Required(vehicleDescription, nameof(vehicleDescription));
        CapacityKg = DomainGuard.Positive(capacityKg, nameof(capacityKg));
        ProfilePhotoUrl = profilePhotoUrl;
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
}
