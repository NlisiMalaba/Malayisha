using Malayisha.Domain.Enums;

namespace Malayisha.Api.Contracts.Admin;

public sealed record ApplyBoostRequest(
    DateTime BoostStartAtUtc,
    DateTime BoostEndAtUtc);

public sealed record AdminReviewDto(
    Guid Id,
    Guid BookingId,
    Guid SenderId,
    Guid TransporterProfileId,
    int Rating,
    string? Comment,
    bool IsHidden,
    DateTime CreatedAtUtc);

public sealed record AdminReviewsResponse(IReadOnlyList<AdminReviewDto> Reviews);

public sealed record CommissionRecordDto(
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

public sealed record CommissionReportResponse(IReadOnlyList<CommissionRecordDto> Records);

public sealed record BoostedTripDto(
    Guid Id,
    Guid TransporterProfileId,
    bool IsBoosted,
    DateTime? BoostStartAtUtc,
    DateTime? BoostEndAtUtc,
    DateTime UpdatedAtUtc);
