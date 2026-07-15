using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.DeliveryRequest;
using Malayisha.Application.Common;
using Malayisha.Application.Features.DeliveryRequest;
using Malayisha.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class DeliveryRequestResultMapper
{
    public static IActionResult ToCreatedResult(Result<DeliveryRequestResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new ObjectResult(ToDto(result.Value)) { StatusCode = StatusCodes.Status201Created }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToResponseResult(Result<DeliveryRequestResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToCancelResult(Result result) =>
        result.IsSuccess
            ? new NoContentResult()
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToListResult(Result<DeliveryRequestPageResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToPageDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    private static DeliveryRequestDto ToDto(DeliveryRequestResponse request) =>
        new(
            request.Id,
            request.SenderId,
            request.OriginCity,
            request.DestinationCity,
            request.RequiredDateUtc,
            request.WeightKg,
            request.SizeDescription,
            request.GoodsDescription,
            request.Status,
            request.CreatedAtUtc,
            request.UpdatedAtUtc);

    private static DeliveryRequestPageDto ToPageDto(DeliveryRequestPageResponse page) =>
        new(
            page.Items.Select(ToDto).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalCount);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = DeliveryRequestErrorMapper.ToStatusCode(errorCode)
        };
}
