namespace Malayisha.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken() { }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        TokenHash = DomainGuard.Required(tokenHash, nameof(tokenHash));
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static RefreshToken Create(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc) =>
        new(id, userId, tokenHash, issuedAtUtc, expiresAtUtc);

    public void MarkUsed(DateTime nowUtc)
    {
        IsUsed = true;
        UpdatedAtUtc = nowUtc;
    }

    public void Revoke(DateTime nowUtc)
    {
        IsRevoked = true;
        UpdatedAtUtc = nowUtc;
    }
}
