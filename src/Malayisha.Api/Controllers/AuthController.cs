using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.RefreshToken;
using Malayisha.Application.Features.Auth.SendOtp;
using Malayisha.Application.Features.Auth.VerifyOtp;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(OtpSentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SendOtpCommand(request.PhoneNumber, OtpPurpose.Register, request.Role),
            cancellationToken);

        return AuthResultMapper.ToSendOtpResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(OtpSentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SendOtpCommand(request.PhoneNumber, OtpPurpose.Login),
            cancellationToken);

        return AuthResultMapper.ToSendOtpResult(result);
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new VerifyOtpCommand(request.PhoneNumber, request.OtpCode, request.Purpose, request.Role),
            cancellationToken);

        return AuthResultMapper.ToSessionResult(result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return AuthResultMapper.ToSessionResult(result);
    }
}
