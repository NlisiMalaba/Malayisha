namespace Malayisha.Domain.Entities;

public sealed class TripListing
{
    private TripListing() { }

    private TripListing(
        Guid id,
        Guid transporterProfileId,
        string originCity,
        string destinationCity,
        DateTime departureDateUtc,
        decimal availableCapacityKg,
        decimal priceGuideZar,
        DateTime createdAtUtc)
    {
        Id = id;
        TransporterProfileId = transporterProfileId;
        OriginCity = DomainGuard.Required(originCity, nameof(originCity));
        DestinationCity = DomainGuard.Required(destinationCity, nameof(destinationCity));
        DepartureDateUtc = departureDateUtc;
        AvailableCapacityKg = DomainGuard.Positive(availableCapacityKg, nameof(availableCapacityKg));
        PriceGuideZar = DomainGuard.Positive(priceGuideZar, nameof(priceGuideZar));
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TransporterProfileId { get; private set; }
    public string OriginCity { get; private set; } = string.Empty;
    public string DestinationCity { get; private set; } = string.Empty;
    public DateTime DepartureDateUtc { get; private set; }
    public decimal AvailableCapacityKg { get; private set; }
    public decimal PriceGuideZar { get; private set; }
    public string? Description { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static TripListing Create(
        Guid id,
        Guid transporterProfileId,
        string originCity,
        string destinationCity,
        DateTime departureDateUtc,
        decimal availableCapacityKg,
        decimal priceGuideZar,
        DateTime nowUtc) =>
        new(id, transporterProfileId, originCity, destinationCity, departureDateUtc, availableCapacityKg, priceGuideZar, nowUtc);

    public void Update(
        string originCity,
        string destinationCity,
        DateTime departureDateUtc,
        decimal availableCapacityKg,
        decimal priceGuideZar,
        string? description,
        DateTime nowUtc)
    {
        OriginCity = DomainGuard.Required(originCity, nameof(originCity));
        DestinationCity = DomainGuard.Required(destinationCity, nameof(destinationCity));
        DepartureDateUtc = departureDateUtc;
        AvailableCapacityKg = DomainGuard.Positive(availableCapacityKg, nameof(availableCapacityKg));
        PriceGuideZar = DomainGuard.Positive(priceGuideZar, nameof(priceGuideZar));
        Description = description;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        UpdatedAtUtc = nowUtc;
    }
}
