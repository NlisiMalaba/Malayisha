using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class DeliveryRequest
{
    private DeliveryRequest() { }

    private DeliveryRequest(
        Guid id,
        Guid senderId,
        string originCity,
        string destinationCity,
        DateTime requiredDateUtc,
        decimal weightKg,
        string sizeDescription,
        string goodsDescription,
        DateTime createdAtUtc)
    {
        Id = id;
        SenderId = senderId;
        OriginCity = DomainGuard.Required(originCity, nameof(originCity));
        DestinationCity = DomainGuard.Required(destinationCity, nameof(destinationCity));
        RequiredDateUtc = requiredDateUtc;
        WeightKg = DomainGuard.Positive(weightKg, nameof(weightKg));
        SizeDescription = DomainGuard.Required(sizeDescription, nameof(sizeDescription));
        GoodsDescription = DomainGuard.Required(goodsDescription, nameof(goodsDescription));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SenderId { get; private set; }
    public string OriginCity { get; private set; } = string.Empty;
    public string DestinationCity { get; private set; } = string.Empty;
    public DateTime RequiredDateUtc { get; private set; }
    public decimal WeightKg { get; private set; }
    public string SizeDescription { get; private set; } = string.Empty;
    public string GoodsDescription { get; private set; } = string.Empty;
    public DeliveryRequestStatus Status { get; private set; } = DeliveryRequestStatus.Active;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static DeliveryRequest Create(
        Guid id,
        Guid senderId,
        string originCity,
        string destinationCity,
        DateTime requiredDateUtc,
        decimal weightKg,
        string sizeDescription,
        string goodsDescription,
        DateTime nowUtc) =>
        new(id, senderId, originCity, destinationCity, requiredDateUtc, weightKg, sizeDescription, goodsDescription, nowUtc);

    public void Update(
        string originCity,
        string destinationCity,
        DateTime requiredDateUtc,
        decimal weightKg,
        string sizeDescription,
        string goodsDescription,
        DateTime nowUtc)
    {
        OriginCity = DomainGuard.Required(originCity, nameof(originCity));
        DestinationCity = DomainGuard.Required(destinationCity, nameof(destinationCity));
        RequiredDateUtc = requiredDateUtc;
        WeightKg = DomainGuard.Positive(weightKg, nameof(weightKg));
        SizeDescription = DomainGuard.Required(sizeDescription, nameof(sizeDescription));
        GoodsDescription = DomainGuard.Required(goodsDescription, nameof(goodsDescription));
        UpdatedAtUtc = nowUtc;
    }

    public void MarkCancelled(DateTime nowUtc)
    {
        Status = DeliveryRequestStatus.Cancelled;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkConvertedToBooking(DateTime nowUtc)
    {
        Status = DeliveryRequestStatus.ConvertedToBooking;
        UpdatedAtUtc = nowUtc;
    }
}
