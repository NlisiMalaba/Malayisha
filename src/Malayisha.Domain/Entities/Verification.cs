using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class Verification
{
    private Verification() { }

    private Verification(Guid id, Guid transporterProfileId, DateTime submittedAtUtc, VerificationStatus status)
    {
        Id = id;
        TransporterProfileId = transporterProfileId;
        SubmittedAtUtc = submittedAtUtc;
        Status = status;
    }

    public Guid Id { get; private set; }
    public Guid TransporterProfileId { get; private set; }
    public VerificationStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public Guid? ReviewedByAdminUserId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    public static Verification Create(Guid id, Guid transporterProfileId, DateTime nowUtc) =>
        new(id, transporterProfileId, nowUtc, VerificationStatus.Pending);

    public void Approve(Guid adminUserId, DateTime nowUtc)
    {
        Status = VerificationStatus.Approved;
        ReviewedByAdminUserId = adminUserId;
        ReviewedAtUtc = nowUtc;
        RejectionReason = null;
    }

    public void Reject(Guid adminUserId, string? reason, DateTime nowUtc)
    {
        Status = VerificationStatus.Rejected;
        ReviewedByAdminUserId = adminUserId;
        ReviewedAtUtc = nowUtc;
        RejectionReason = reason;
    }
}
