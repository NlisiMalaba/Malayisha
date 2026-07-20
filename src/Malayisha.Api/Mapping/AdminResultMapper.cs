using Malayisha.Api.Contracts.Admin;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Application.Common;
using ApplicationAdminReviewDto = Malayisha.Application.Features.Review.AdminReviewDto;
using ApplicationBoostedTripDto = Malayisha.Application.Features.Trip.BoostedTripDto;
using ApplicationCommissionDto = Malayisha.Application.Features.Commission.CommissionDto;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class AdminResultMapper
{
    public static IActionResult ToAdminReviewsResult(Result<IReadOnlyList<ApplicationAdminReviewDto>> result) =>
        result.IsSuccess
            ? new OkObjectResult(new AdminReviewsResponse(
                (result.Value ?? []).Select(ToAdminReviewDto).ToArray()))
            : ToReviewErrorResult(result.ErrorCode);

    public static IActionResult ToAdminReviewResult(Result<ApplicationAdminReviewDto> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToAdminReviewDto(result.Value))
            : ToReviewErrorResult(result.ErrorCode);

    public static IActionResult ToCommissionReportResult(Result<IReadOnlyList<ApplicationCommissionDto>> result) =>
        result.IsSuccess
            ? new OkObjectResult(new CommissionReportResponse(
                (result.Value ?? []).Select(ToCommissionDto).ToArray()))
            : ToCommissionErrorResult(result.ErrorCode);

    public static IActionResult ToCommissionResult(Result<ApplicationCommissionDto> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToCommissionDto(result.Value))
            : ToCommissionErrorResult(result.ErrorCode);

    public static IActionResult ToBoostedTripResult(Result<ApplicationBoostedTripDto> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToBoostedTripDto(result.Value))
            : ToTripErrorResult(result.ErrorCode);

    private static AdminReviewDto ToAdminReviewDto(ApplicationAdminReviewDto review) =>
        new(
            review.Id,
            review.BookingId,
            review.SenderId,
            review.TransporterProfileId,
            review.Rating,
            review.Comment,
            review.IsHidden,
            review.CreatedAtUtc);

    private static CommissionRecordDto ToCommissionDto(ApplicationCommissionDto record) =>
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

    private static BoostedTripDto ToBoostedTripDto(ApplicationBoostedTripDto trip) =>
        new(
            trip.Id,
            trip.TransporterProfileId,
            trip.IsBoosted,
            trip.BoostStartAtUtc,
            trip.BoostEndAtUtc,
            trip.UpdatedAtUtc);

    private static ObjectResult ToReviewErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = ReviewErrorMapper.ToStatusCode(errorCode)
        };

    private static ObjectResult ToCommissionErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = CommissionErrorMapper.ToStatusCode(errorCode)
        };

    private static ObjectResult ToTripErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = TripErrorMapper.ToStatusCode(errorCode)
        };
}
