namespace Malayisha.Application.Features.Profile;

internal static class ProfileValidation
{
    public const int DisplayNameMaxLength = 120;
    public const int VehicleDescriptionMaxLength = 500;
    public const int RouteMaxLength = 120;
    public const int MaxRoutes = 20;
    public const int ProfilePhotoUrlMaxLength = 2048;
    public const string ProfilePhotoCategory = "profile-photos";

    public static readonly HashSet<string> AllowedPhotoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
}
