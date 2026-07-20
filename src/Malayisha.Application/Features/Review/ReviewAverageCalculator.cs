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
}
