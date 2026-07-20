using System.Reflection;
using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Application.Features.Booking.CompleteBooking;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Behaviors;

internal sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserAccessor currentUserAccessor,
    ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (ShouldBypass(request))
        {
            return await next(cancellationToken);
        }

        var authorizeAttribute = request.GetType().GetCustomAttribute<AuthorizeRolesAttribute>();
        if (authorizeAttribute is null || authorizeAttribute.Roles.Count == 0)
        {
            return await next(cancellationToken);
        }

        if (!currentUserAccessor.IsAuthenticated || currentUserAccessor.UserId is null)
        {
            logger.LogWarning(
                "Unauthorized access attempt for {RequestName}",
                typeof(TRequest).Name);

            return ResultResponseFactory.Unauthorized<TResponse>();
        }

        if (!currentUserAccessor.Roles.Any(authorizeAttribute.Roles.Contains))
        {
            logger.LogWarning(
                "Forbidden access attempt for {RequestName} by user {UserId} with roles {Roles}",
                typeof(TRequest).Name,
                currentUserAccessor.UserId,
                string.Join(',', currentUserAccessor.Roles));

            return ResultResponseFactory.Forbidden<TResponse>();
        }

        return await next(cancellationToken);
    }

    private static bool ShouldBypass(TRequest request) =>
        request switch
        {
            CompleteBookingCommand { IsSystemAction: true } => true,
            _ => false
        };
}
