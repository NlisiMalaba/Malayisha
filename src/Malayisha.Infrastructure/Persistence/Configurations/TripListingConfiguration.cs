using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class TripListingConfiguration : IEntityTypeConfiguration<TripListing>
{
    public void Configure(EntityTypeBuilder<TripListing> builder)
    {
        builder.ToTable("trip_listings");

        builder.HasKey(trip => trip.Id);

        builder.Property(trip => trip.OriginCity)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(trip => trip.DestinationCity)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(trip => trip.DepartureDateUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(trip => trip.AvailableCapacityKg)
            .HasWeightPrecision()
            .IsRequired();

        builder.Property(trip => trip.PriceGuideZar)
            .HasMoneyPrecision()
            .IsRequired();

        builder.Property(trip => trip.Description)
            .HasMaxLength(2000);

        builder.Property(trip => trip.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(trip => trip.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<TransporterProfile>()
            .WithMany()
            .HasForeignKey(trip => trip.TransporterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(trip => new { trip.OriginCity, trip.DestinationCity, trip.DepartureDateUtc })
            .HasDatabaseName("idx_trip_search")
            .HasFilter("is_deleted = false");
    }
}
