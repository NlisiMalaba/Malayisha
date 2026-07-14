using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Verification;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Verification;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class VerificationResultMapper
{
    public static IActionResult ToCreatedResult(Result<VerificationResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new ObjectResult(ToDto(result.Value)) { StatusCode = StatusCodes.Status201Created }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToResult(Result<VerificationResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToPendingListResult(Result<IReadOnlyList<PendingVerificationResponse>> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(result.Value.Select(ToPendingDto).ToArray())
            : ToErrorResult(result.ErrorCode);

    private static VerificationDto ToDto(VerificationResponse verification) =>
        new(
            verification.Id,
            verification.TransporterProfileId,
            verification.Status,
            verification.SubmittedAtUtc,
            verification.ReviewedByAdminUserId,
            verification.ReviewedAtUtc,
            verification.RejectionReason);

    private static PendingVerificationDto ToPendingDto(PendingVerificationResponse verification) =>
        new(
            verification.Id,
            verification.SubmittedAtUtc,
            new PendingVerificationProfileDto(
                verification.Profile.Id,
                verification.Profile.DisplayName,
                verification.Profile.RoutesServed,
                verification.Profile.VehicleDescription,
                verification.Profile.CapacityKg,
                verification.Profile.ProfilePhotoUrl,
                verification.Profile.IsVerified,
                verification.Profile.AverageRating));

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = VerificationErrorMapper.ToStatusCode(errorCode)
        };
}
