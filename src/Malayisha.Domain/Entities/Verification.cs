using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class Verification
{
    public const string InvalidStateTransitionError = "InvalidVerificationStatus";

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

    public bool IsActive =>
        Status is VerificationStatus.Pending or VerificationStatus.Approved;

    public static Verification Create(Guid id, Guid transporterProfileId, DateTime nowUtc) =>
        new(id, transporterProfileId, nowUtc, VerificationStatus.Pending);

    public Result Approve(Guid adminUserId, DateTime nowUtc)
    {
        if (Status != VerificationStatus.Pending)
        {
            return Result.Error(InvalidStateTransitionError);
        }

        Status = VerificationStatus.Approved;
        ReviewedByAdminUserId = adminUserId;
        ReviewedAtUtc = nowUtc;
        RejectionReason = null;
        return Result.Success();
    }

    public Result Reject(Guid adminUserId, string? reason, DateTime nowUtc)
    {
        if (Status != VerificationStatus.Pending)
        {
            return Result.Error(InvalidStateTransitionError);
        }

        Status = VerificationStatus.Rejected;
        ReviewedByAdminUserId = adminUserId;
        ReviewedAtUtc = nowUtc;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return Result.Success();
    }
}
