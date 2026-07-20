using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class CommissionRecord
{
    public const string InvalidStateTransitionError = "InvalidCommissionStatus";

    private CommissionRecord() { }

    private CommissionRecord(
        Guid id,
        Guid bookingId,
        Guid transporterUserId,
        decimal agreedPriceZar,
        decimal commissionRate,
        DateTime completionDateUtc)
    {
        Id = id;
        BookingId = bookingId;
        TransporterUserId = transporterUserId;
        AgreedPriceZar = DomainGuard.Positive(agreedPriceZar, nameof(agreedPriceZar));
        CommissionRate = DomainGuard.Positive(commissionRate, nameof(commissionRate));
        CompletionDateUtc = completionDateUtc;
        CommissionAmountZar = decimal.Round(AgreedPriceZar * CommissionRate, 2, MidpointRounding.AwayFromZero);
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid TransporterUserId { get; private set; }
    public decimal AgreedPriceZar { get; private set; }
    public decimal CommissionRate { get; private set; }
    public decimal CommissionAmountZar { get; private set; }
    public CommissionStatus Status { get; private set; } = CommissionStatus.Pending;
    public Guid? UpdatedByAdminUserId { get; private set; }
    public DateTime CompletionDateUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static CommissionRecord Create(
        Guid id,
        Guid bookingId,
        Guid transporterUserId,
        decimal agreedPriceZar,
        decimal commissionRate,
        DateTime completionDateUtc) =>
        new(id, bookingId, transporterUserId, agreedPriceZar, commissionRate, completionDateUtc);

    public Result MarkInvoiced(Guid adminUserId, DateTime nowUtc)
    {
        if (Status != CommissionStatus.Pending)
        {
            return Result.Error(InvalidStateTransitionError);
        }

        Status = CommissionStatus.Invoiced;
        UpdatedByAdminUserId = adminUserId;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    public Result MarkPaid(Guid adminUserId, DateTime nowUtc)
    {
        if (Status != CommissionStatus.Invoiced)
        {
            return Result.Error(InvalidStateTransitionError);
        }

        Status = CommissionStatus.Paid;
        UpdatedByAdminUserId = adminUserId;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }
}
