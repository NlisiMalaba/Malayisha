namespace Malayisha.Domain.Entities;

public sealed class OtpRecord
{
    private OtpRecord() { }

    private OtpRecord(
        Guid id,
        string phoneNumber,
        string otpHash,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc)
    {
        Id = id;
        PhoneNumber = DomainGuard.Required(phoneNumber, nameof(phoneNumber));
        OtpHash = DomainGuard.Required(otpHash, nameof(otpHash));
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string OtpHash { get; private set; } = string.Empty;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }
    public bool IsConsumed { get; private set; }

    public static OtpRecord Create(
        Guid id,
        string phoneNumber,
        string otpHash,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc) =>
        new(id, phoneNumber, otpHash, issuedAtUtc, expiresAtUtc);

    public void RecordFailure() => FailedAttempts++;

    public void Consume() => IsConsumed = true;
}
