using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Profile;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Profile.CreateProfile;
using Malayisha.Application.Features.Profile.GetPublicProfile;
using Malayisha.Application.Features.Profile.UpdateProfile;
using Malayisha.Application.Features.Profile.UploadPhoto;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/profile")]
public sealed class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(TransporterProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CreateProfileCommand(
                userId,
                request.DisplayName,
                request.RoutesServed,
                request.VehicleDescription,
                request.CapacityKg,
                request.ProfilePhotoUrl),
            cancellationToken);

        return ProfileResultMapper.ToCreatedResult(result);
    }

    [HttpPut]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(TransporterProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new UpdateProfileCommand(
                userId,
                request.DisplayName,
                request.RoutesServed,
                request.VehicleDescription,
                request.CapacityKg),
            cancellationToken);

        return ProfileResultMapper.ToOwnerResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicTransporterProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublic(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPublicProfileQuery(id), cancellationToken);
        return ProfileResultMapper.ToPublicResult(result);
    }

    [HttpPost("photo")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(UploadProfilePhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(
        [FromBody] UploadProfilePhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new UploadPhotoCommand(userId, request.FileName, request.ContentType),
            cancellationToken);

        return ProfileResultMapper.ToUploadResult(result);
    }
}
