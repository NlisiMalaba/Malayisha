using Malayisha.Application.Behaviors;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Commission;
using Malayisha.Application.Features.Review;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Verification;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests.Support;

internal static class AuthorizationTestHelper
{
    public static async Task<(Domain.Common.IResultResponse Response, bool HandlerInvoked)> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        StubCurrentUserAccessor accessor,
        CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        var handlerInvoked = false;
        var behavior = new AuthorizationBehavior<TRequest, TResponse>(
            accessor,
            NullLogger<AuthorizationBehavior<TRequest, TResponse>>.Instance);

        var response = await behavior.Handle(
            request,
            cancellationToken =>
            {
                handlerInvoked = true;
                return Task.FromResult(CreateSuccessResponse<TResponse>());
            },
            cancellationToken);

        return ((Domain.Common.IResultResponse)response!, handlerInvoked);
    }

    private static TResponse CreateSuccessResponse<TResponse>()
    {
        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            throw new InvalidOperationException($"Unsupported response type {responseType.Name}.");
        }

        var valueType = responseType.GetGenericArguments()[0];
        var value = CreateSuccessValue(valueType);
        var success = responseType.GetMethod(nameof(Result<object>.Success), [valueType])!;
        return (TResponse)success.Invoke(null, [value])!;
    }

    private static object? CreateSuccessValue(Type valueType)
    {
        if (valueType == typeof(VerificationResponse))
        {
            return new VerificationResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                VerificationStatus.Pending,
                DateTime.UtcNow,
                null,
                null,
                null);
        }

        if (valueType == typeof(ReviewDto))
        {
            return new ReviewDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                5,
                null,
                DateTime.UtcNow);
        }

        if (valueType == typeof(AdminReviewDto))
        {
            return new AdminReviewDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                5,
                null,
                false,
                DateTime.UtcNow);
        }

        if (valueType == typeof(CommissionDto))
        {
            return new CommissionDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                100m,
                0.08m,
                8m,
                CommissionStatus.Pending,
                null,
                DateTime.UtcNow,
                null);
        }

        if (valueType == typeof(BoostedTripDto))
        {
            return new BoostedTripDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                true,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow);
        }

        if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            var elementType = valueType.GetGenericArguments()[0];
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        }

        return Activator.CreateInstance(valueType);
    }
}
