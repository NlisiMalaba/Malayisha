namespace Malayisha.Application.Features.Trip;

internal static class TripValidation
{
    public const int CityMaxLength = 120;
    public const int DescriptionMaxLength = 2000;
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public static readonly TimeSpan SearchCacheTtl = TimeSpan.FromSeconds(60);
}
