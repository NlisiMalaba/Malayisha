using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Profile;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Profile;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class ProfileResultMapper
{
    public static IActionResult ToCreatedResult(Result<TransporterProfileResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new ObjectResult(ToOwnerDto(result.Value)) { StatusCode = StatusCodes.Status201Created }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToOwnerResult(Result<TransporterProfileResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToOwnerDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToPublicResult(Result<PublicTransporterProfileResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToPublicDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToUploadResult(Result<UploadProfilePhotoResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToUploadDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    private static TransporterProfileDto ToOwnerDto(TransporterProfileResponse profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.RoutesServed,
            profile.VehicleDescription,
            profile.CapacityKg,
            profile.ProfilePhotoUrl,
            profile.IsVerified,
            profile.AverageRating,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);

    private static PublicTransporterProfileDto ToPublicDto(PublicTransporterProfileResponse profile) =>
        new(
            profile.Id,
            profile.DisplayName,
            profile.RoutesServed,
            profile.VehicleDescription,
            profile.CapacityKg,
            profile.ProfilePhotoUrl,
            profile.IsVerified,
            profile.AverageRating);

    private static UploadProfilePhotoDto ToUploadDto(UploadProfilePhotoResponse upload) =>
        new(
            upload.UploadUrl,
            upload.ObjectKey,
            upload.CdnUrl,
            upload.ExpiresAtUtc,
            upload.ProfilePhotoUrl);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = ProfileErrorMapper.ToStatusCode(errorCode)
        };
}
