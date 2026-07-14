using Malayisha.Application.Common;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Auth.VerifyOtp;

public sealed record VerifyOtpCommand(
    string PhoneNumber,
    string OtpCode,
    OtpPurpose Purpose,
    UserRole? Role = null) : IRequest<Result<AuthSessionResponse>>;
