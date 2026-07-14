using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthSessionResponse>>;
