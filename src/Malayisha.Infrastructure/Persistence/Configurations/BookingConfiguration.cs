using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(booking => booking.QuotedPriceZar)
            .HasMoneyPrecision();

        builder.Property(booking => booking.AgreedPriceZar)
            .HasMoneyPrecision();

        builder.Property(booking => booking.Message)
            .HasMaxLength(2000);

        builder.Property(booking => booking.InTransitAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(booking => booking.DeliveredAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(booking => booking.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(booking => booking.CancelledAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(booking => booking.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(booking => booking.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<TripListing>()
            .WithMany()
            .HasForeignKey(booking => booking.TripListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DeliveryRequest>()
            .WithMany()
            .HasForeignKey(booking => booking.DeliveryRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(booking => booking.DeliveryRequestId)
            .IsUnique()
            .HasFilter("delivery_request_id IS NOT NULL")
            .HasDatabaseName("ix_bookings_delivery_request_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(booking => booking.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(booking => booking.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(booking => booking.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(booking => new { booking.TransporterId, booking.Status, booking.CreatedAtUtc })
            .HasDatabaseName("idx_booking_transporter");
    }
}
