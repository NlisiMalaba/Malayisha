using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Trip;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Trip.GetShareLink;
using Malayisha.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class TripResultMapper
{
    public static IActionResult ToCreatedResult(Result<TripListingResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new ObjectResult(ToListingDto(result.Value)) { StatusCode = StatusCodes.Status201Created }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToListingResult(Result<TripListingResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToListingDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToDeleteResult(Result result) =>
        result.IsSuccess
            ? new NoContentResult()
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToSearchResult(Result<TripSearchPageResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToSearchPageDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToShareLinkResult(Result<ShareLinkResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(new ShareLinkDto(result.Value.Url))
            : ToErrorResult(result.ErrorCode);

    private static TripListingDto ToListingDto(TripListingResponse trip) =>
        new(
            trip.Id,
            trip.TransporterProfileId,
            trip.OriginCity,
            trip.DestinationCity,
            trip.DepartureDateUtc,
            trip.AvailableCapacityKg,
            trip.PriceGuideZar,
            trip.Description,
            trip.IsDeleted,
            trip.CreatedAtUtc,
            trip.UpdatedAtUtc);

    private static TripSearchPageDto ToSearchPageDto(TripSearchPageResponse page) =>
        new(
            page.Items.Select(ToSearchItemDto).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalCount);

    private static TripSearchItemDto ToSearchItemDto(TripSearchItemResponse item) =>
        new(
            item.Id,
            item.OriginCity,
            item.DestinationCity,
            item.DepartureDateUtc,
            item.AvailableCapacityKg,
            item.PriceGuideZar,
            item.IsBoosted,
            new TripSearchTransporterDto(
                item.Transporter.Id,
                item.Transporter.DisplayName,
                item.Transporter.IsVerified,
                item.Transporter.AverageRating,
                item.Transporter.ProfilePhotoUrl));

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = TripErrorMapper.ToStatusCode(errorCode)
        };
}
