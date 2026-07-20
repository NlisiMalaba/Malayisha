using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class User
{
    private User() { }

    private User(Guid id, string phoneNumber, UserRole role, DateTime createdAtUtc)
    {
        Id = id;
        PhoneNumber = DomainGuard.Required(phoneNumber, nameof(phoneNumber));
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? PushDeviceToken { get; private set; }
    public bool MarketingNotificationsOptIn { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static User Create(Guid id, string phoneNumber, UserRole role, DateTime nowUtc) =>
        new(id, phoneNumber, role, nowUtc);

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    public void UpdatePushDeviceToken(string? deviceToken, DateTime nowUtc)
    {
        PushDeviceToken = string.IsNullOrWhiteSpace(deviceToken) ? null : deviceToken.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void SetMarketingNotificationsOptIn(bool optIn, DateTime nowUtc)
    {
        MarketingNotificationsOptIn = optIn;
        UpdatedAtUtc = nowUtc;
    }
}
