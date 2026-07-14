using Malayisha.Domain.Common;
using MediatR;

namespace Malayisha.Application.Features.Auth.SendOtp;

public sealed record SendOtpCommand(
    string PhoneNumber,
    OtpPurpose Purpose,
    Domain.Enums.UserRole? Role = null) : IRequest<Result>;