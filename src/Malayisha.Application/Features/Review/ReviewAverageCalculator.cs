namespace Malayisha.Application.Features.Review;

internal static class ReviewAverageCalculator
{
    internal static decimal Calculate(IEnumerable<int> ratings)
    {
        var values = ratings.ToArray();
        if (values.Length == 0)
        {
            return 0m;
        }

        return Math.Round(values.Average(rating => (decimal)rating), 2, MidpointRounding.AwayFromZero);
    }

    internal static decimal CalculateAfterHiding(IReadOnlyList<int> publicRatings, int hiddenRating)
    {
        var ratings = publicRatings.ToList();
        var index = ratings.IndexOf(hiddenRating);
        if (index >= 0)
        {
            ratings.RemoveAt(index);
        }

        return Calculate(ratings);
    }

    internal static decimal CalculateAfterRestoring(IReadOnlyList<int> publicRatings, int restoredRating) =>
        Calculate(publicRatings.Append(restoredRating));
}
