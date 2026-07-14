using System.Globalization;
using System.Text;
using Malayisha.Domain.Entities;

namespace Malayisha.Application.Features.Trip;

internal static class WhatsAppShareLinkBuilder
{
    private const string WhatsAppShareBaseUrl = "https://wa.me/?text=";

    public static string BuildUrl(
        TripListing trip,
        TransporterProfile transporter,
        string tripDeepLinkBaseUrl)
    {
        var deepLink = BuildDeepLink(tripDeepLinkBaseUrl, trip.Id);
        var message = BuildMessage(trip, transporter, deepLink);
        return WhatsAppShareBaseUrl + Uri.EscapeDataString(message);
    }

    public static string BuildDeepLink(string tripDeepLinkBaseUrl, Guid tripListingId)
    {
        var baseUrl = tripDeepLinkBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{tripListingId:D}";
    }

    public static string BuildMessage(
        TripListing trip,
        TransporterProfile transporter,
        string deepLink)
    {
        var builder = new StringBuilder();
        builder.AppendLine("🚚 oMalayisha Trip Available!");

        builder.Append("Driver: ");
        builder.Append(transporter.DisplayName);
        if (transporter.IsVerified)
        {
            builder.Append(" ✅");
        }

        builder.AppendLine();
        builder.Append("Route: ");
        builder.Append(trip.OriginCity);
        builder.Append(" → ");
        builder.AppendLine(trip.DestinationCity);

        builder.Append("Date: ");
        builder.AppendLine(FormatDepartureDate(trip.DepartureDateUtc));

        builder.Append("Space: ");
        builder.Append(FormatCapacity(trip.AvailableCapacityKg));
        builder.AppendLine(" kg available");

        builder.Append("Price guide: R");
        builder.AppendLine(FormatPrice(trip.PriceGuideZar));

        builder.Append("Book here: ");
        builder.Append(deepLink);

        return builder.ToString();
    }

    private static string FormatDepartureDate(DateTime departureDateUtc) =>
        DateOnly.FromDateTime(departureDateUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatCapacity(decimal availableCapacityKg) =>
        availableCapacityKg.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatPrice(decimal priceGuideZar) =>
        priceGuideZar.ToString("0.##", CultureInfo.InvariantCulture);
}
