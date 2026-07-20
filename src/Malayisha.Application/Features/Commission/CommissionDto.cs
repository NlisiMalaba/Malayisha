using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.Commission;

public sealed record CommissionDto(
    Guid Id,
    Guid BookingId,
    Guid TransporterUserId,
    decimal AgreedPriceZar,
    decimal CommissionRate,
    decimal CommissionAmountZar,
    CommissionStatus Status,
    Guid? UpdatedByAdminUserId,
    DateTime CompletionDateUtc,
    DateTime? UpdatedAtUtc);
