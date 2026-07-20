using FsCheck.Xunit;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Commission;
using Malayisha.Application.Features.Commission.GetCommissionReport;
using Malayisha.Application.Features.Commission.InvoiceCommission;
using Malayisha.Application.Features.Commission.MarkCommissionPaid;
using Malayisha.Application.Features.Review;
using Malayisha.Application.Features.Review.CreateReview;
using Malayisha.Application.Features.Review.GetAllReviews;
using Malayisha.Application.Features.Review.HideReview;
using Malayisha.Application.Features.Review.RestoreReview;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Trip.ApplyBoost;
using Malayisha.Application.Features.Trip.RemoveBoost;
using Malayisha.Application.Features.Verification;
using Malayisha.Application.Features.Verification.ApplyForVerification;
using Malayisha.Application.Features.Verification.ApproveVerification;
using Malayisha.Application.Features.Verification.GetPendingVerifications;
using Malayisha.Application.Features.Verification.RejectVerification;
using Malayisha.Application.Tests.Support;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Tests;

public sealed class RbacPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
    private const int ProtectedEndpointCount = 13;

    [Property(MaxTest = 150)]
    public bool Property34_AuthenticatedWrongRole_ReturnsForbidden(
        int endpointSeed,
        ArbitraryRole callerRole)
    {
        var endpointIndex = Math.Abs(endpointSeed) % ProtectedEndpointCount;
        var allowedRoles = GetAllowedRoles(endpointIndex);
        var callerUserRole = ToUserRole(callerRole);

        if (allowedRoles.Contains(callerUserRole))
        {
            return true;
        }

        var accessor = StubCurrentUserAccessor.Authenticated(BuildUserId(endpointSeed), callerUserRole);
        var (response, handlerInvoked) = InvokeProtectedEndpoint(endpointIndex, accessor)
            .GetAwaiter()
            .GetResult();

        return !handlerInvoked
               && !response.IsSuccess
               && response.ErrorCode == ApplicationErrorCodes.Forbidden;
    }

    [Property(MaxTest = 150)]
    public bool Property34_AuthenticatedAllowedRole_PassesAuthorization(
        int endpointSeed,
        ArbitraryRole callerRole)
    {
        var endpointIndex = Math.Abs(endpointSeed) % ProtectedEndpointCount;
        var allowedRoles = GetAllowedRoles(endpointIndex);
        var callerUserRole = ToUserRole(callerRole);

        if (!allowedRoles.Contains(callerUserRole))
        {
            return true;
        }

        var accessor = StubCurrentUserAccessor.Authenticated(BuildUserId(endpointSeed), callerUserRole);
        var (response, handlerInvoked) = InvokeProtectedEndpoint(endpointIndex, accessor)
            .GetAwaiter()
            .GetResult();

        return handlerInvoked && response.IsSuccess;
    }

    [Property(MaxTest = 100)]
    public bool Property34_Unauthenticated_ReturnsUnauthorized(int endpointSeed)
    {
        var endpointIndex = Math.Abs(endpointSeed) % ProtectedEndpointCount;
        var accessor = StubCurrentUserAccessor.Unauthenticated();
        var (response, handlerInvoked) = InvokeProtectedEndpoint(endpointIndex, accessor)
            .GetAwaiter()
            .GetResult();

        return !handlerInvoked
               && !response.IsSuccess
               && response.ErrorCode == ApplicationErrorCodes.Unauthorized;
    }

    private static UserRole[] GetAllowedRoles(int endpointIndex) =>
        endpointIndex switch
        {
            0 => [UserRole.Transporter],
            1 or 2 or 3 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 => [UserRole.Admin],
            4 => [UserRole.Sender],
            _ => throw new ArgumentOutOfRangeException(nameof(endpointIndex))
        };

    private static async Task<(Domain.Common.IResultResponse Response, bool HandlerInvoked)> InvokeProtectedEndpoint(
        int endpointIndex,
        StubCurrentUserAccessor accessor)
    {
        var entityId = BuildGuid(endpointIndex + 1);
        var adminUserId = BuildUserId(endpointIndex ^ 0xABCDEF);
        var userId = BuildUserId(endpointIndex ^ 0x123456);

        return endpointIndex switch
        {
            0 => await AuthorizationTestHelper.ExecuteAsync<ApplyForVerificationCommand, Result<VerificationResponse>>(
                new ApplyForVerificationCommand(userId),
                accessor),
            1 => await AuthorizationTestHelper.ExecuteAsync<GetPendingVerificationsQuery, Result<IReadOnlyList<PendingVerificationResponse>>>(
                new GetPendingVerificationsQuery(),
                accessor),
            2 => await AuthorizationTestHelper.ExecuteAsync<ApproveVerificationCommand, Result<VerificationResponse>>(
                new ApproveVerificationCommand(entityId, adminUserId),
                accessor),
            3 => await AuthorizationTestHelper.ExecuteAsync<RejectVerificationCommand, Result<VerificationResponse>>(
                new RejectVerificationCommand(entityId, adminUserId, "Incomplete documentation"),
                accessor),
            4 => await AuthorizationTestHelper.ExecuteAsync<CreateReviewCommand, Result<ReviewDto>>(
                new CreateReviewCommand(userId, entityId, 5, null),
                accessor),
            5 => await AuthorizationTestHelper.ExecuteAsync<GetAllReviewsQuery, Result<IReadOnlyList<AdminReviewDto>>>(
                new GetAllReviewsQuery(),
                accessor),
            6 => await AuthorizationTestHelper.ExecuteAsync<HideReviewCommand, Result<AdminReviewDto>>(
                new HideReviewCommand(entityId, adminUserId),
                accessor),
            7 => await AuthorizationTestHelper.ExecuteAsync<RestoreReviewCommand, Result<AdminReviewDto>>(
                new RestoreReviewCommand(entityId, adminUserId),
                accessor),
            8 => await AuthorizationTestHelper.ExecuteAsync<GetCommissionReportQuery, Result<IReadOnlyList<CommissionDto>>>(
                new GetCommissionReportQuery(null, null, null),
                accessor),
            9 => await AuthorizationTestHelper.ExecuteAsync<InvoiceCommissionCommand, Result<CommissionDto>>(
                new InvoiceCommissionCommand(entityId, adminUserId),
                accessor),
            10 => await AuthorizationTestHelper.ExecuteAsync<MarkCommissionPaidCommand, Result<CommissionDto>>(
                new MarkCommissionPaidCommand(entityId, adminUserId),
                accessor),
            11 => await AuthorizationTestHelper.ExecuteAsync<ApplyBoostCommand, Result<BoostedTripDto>>(
                new ApplyBoostCommand(entityId, adminUserId, BaselineUtc, BaselineUtc.AddDays(7)),
                accessor),
            12 => await AuthorizationTestHelper.ExecuteAsync<RemoveBoostCommand, Result<BoostedTripDto>>(
                new RemoveBoostCommand(entityId, adminUserId),
                accessor),
            _ => throw new ArgumentOutOfRangeException(nameof(endpointIndex))
        };
    }

    private static UserRole ToUserRole(ArbitraryRole role) =>
        role switch
        {
            ArbitraryRole.Sender => UserRole.Sender,
            ArbitraryRole.Transporter => UserRole.Transporter,
            ArbitraryRole.Admin => UserRole.Admin,
            _ => UserRole.Admin
        };

    private static Guid BuildUserId(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private static Guid BuildGuid(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x13579BDF);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 17);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed ^ 0x2468ACE0);
        return new Guid(bytes);
    }
}
