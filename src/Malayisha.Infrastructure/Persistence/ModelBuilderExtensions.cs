using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence;

internal static class ModelBuilderExtensions
{
    internal static PropertyBuilder<decimal> HasMoneyPrecision(this PropertyBuilder<decimal> builder) =>
        builder.HasPrecision(18, 2);

    internal static PropertyBuilder<decimal?> HasMoneyPrecision(this PropertyBuilder<decimal?> builder) =>
        builder.HasPrecision(18, 2);

    internal static PropertyBuilder<decimal> HasWeightPrecision(this PropertyBuilder<decimal> builder) =>
        builder.HasPrecision(10, 2);
}
