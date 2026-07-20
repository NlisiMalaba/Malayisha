namespace Malayisha.Application.Features.Commission;

internal static class CommissionMappings
{
    internal static CommissionDto ToDto(Domain.Entities.CommissionRecord record) =>
        new(
            record.Id,
            record.BookingId,
            record.TransporterUserId,
            record.AgreedPriceZar,
            record.CommissionRate,
            record.CommissionAmountZar,
            record.Status,
            record.UpdatedByAdminUserId,
            record.CompletionDateUtc,
            record.UpdatedAtUtc);
}
