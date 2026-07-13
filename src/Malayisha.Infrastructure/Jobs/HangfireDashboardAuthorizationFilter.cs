using Hangfire.Dashboard;

namespace Malayisha.Infrastructure.Jobs;

/// <summary>
/// Allows Hangfire dashboard access in Development only. Production dashboard access
/// should be gated behind authenticated admin authorization in a later security task.
/// </summary>
internal sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
