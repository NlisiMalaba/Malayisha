using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Admin;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Verification;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Commission.GetCommissionReport;
using Malayisha.Application.Features.Commission.InvoiceCommission;
using Malayisha.Application.Features.Commission.MarkCommissionPaid;
using Malayisha.Application.Features.Review.GetAllReviews;
using Malayisha.Application.Features.Review.HideReview;
using Malayisha.Application.Features.Review.RestoreReview;
using Malayisha.Application.Features.Trip.ApplyBoost;
using Malayisha.Application.Features.Trip.RemoveBoost;
using Malayisha.Application.Features.Verification.ApproveVerification;
using Malayisha.Application.Features.Verification.GetPendingVerifications;
using Malayisha.Application.Features.Verification.RejectVerification;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    [HttpGet("verifications/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingVerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingVerifications(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPendingVerificationsQuery(), cancellationToken);
        return VerificationResultMapper.ToPendingListResult(result);
    }

    [HttpPost("verifications/{id:guid}/approve")]
    [ProducesResponseType(typeof(VerificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ApproveVerification(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new ApproveVerificationCommand(id, adminUserId),
            cancellationToken);

        return VerificationResultMapper.ToResult(result);
    }

    [HttpPost("verifications/{id:guid}/reject")]
    [ProducesResponseType(typeof(VerificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RejectVerification(
        Guid id,
        [FromBody] RejectVerificationRequest? request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new RejectVerificationCommand(id, adminUserId, request?.RejectionReason),
            cancellationToken);

        return VerificationResultMapper.ToResult(result);
    }

    [HttpGet("reviews")]
    [ProducesResponseType(typeof(AdminReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllReviews(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllReviewsQuery(), cancellationToken);
        return AdminResultMapper.ToAdminReviewsResult(result);
    }

    [HttpPost("reviews/{id:guid}/hide")]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> HideReview(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new HideReviewCommand(id, adminUserId), cancellationToken);
        return AdminResultMapper.ToAdminReviewResult(result);
    }

    [HttpPost("reviews/{id:guid}/restore")]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RestoreReview(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new RestoreReviewCommand(id, adminUserId), cancellationToken);
        return AdminResultMapper.ToAdminReviewResult(result);
    }

    [HttpGet("commission")]
    [ProducesResponseType(typeof(CommissionReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCommissionReport(
        [FromQuery] CommissionStatus? status,
        [FromQuery] DateTime? fromCompletionDateUtc,
        [FromQuery] DateTime? toCompletionDateUtc,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetCommissionReportQuery(status, fromCompletionDateUtc, toCompletionDateUtc),
            cancellationToken);

        return AdminResultMapper.ToCommissionReportResult(result);
    }

    [HttpPost("commission/{id:guid}/invoice")]
    [ProducesResponseType(typeof(CommissionRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> InvoiceCommission(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new InvoiceCommissionCommand(id, adminUserId),
            cancellationToken);

        return AdminResultMapper.ToCommissionResult(result);
    }

    [HttpPost("commission/{id:guid}/paid")]
    [ProducesResponseType(typeof(CommissionRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkCommissionPaid(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new MarkCommissionPaidCommand(id, adminUserId),
            cancellationToken);

        return AdminResultMapper.ToCommissionResult(result);
    }

    [HttpPost("boosts/{tripListingId:guid}/apply")]
    [ProducesResponseType(typeof(BoostedTripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyBoost(
        Guid tripListingId,
        [FromBody] ApplyBoostRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new ApplyBoostCommand(
                tripListingId,
                adminUserId,
                request.BoostStartAtUtc,
                request.BoostEndAtUtc),
            cancellationToken);

        return AdminResultMapper.ToBoostedTripResult(result);
    }

    [HttpPost("boosts/{tripListingId:guid}/remove")]
    [ProducesResponseType(typeof(BoostedTripDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveBoost(Guid tripListingId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new RemoveBoostCommand(tripListingId, adminUserId),
            cancellationToken);

        return AdminResultMapper.ToBoostedTripResult(result);
    }
}
