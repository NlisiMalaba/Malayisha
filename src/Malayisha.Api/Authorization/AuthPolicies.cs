namespace Malayisha.Api.Authorization;

public static class AuthPolicies
{
    public const string SenderOnly = nameof(SenderOnly);

    public const string TransporterOnly = nameof(TransporterOnly);

    public const string SenderOrTransporter = nameof(SenderOrTransporter);

    public const string AdminOnly = nameof(AdminOnly);

    public const string Authenticated = nameof(Authenticated);
}
