using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Rating)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(2000);

        builder.Property(review => review.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(review => review.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(review => review.BookingId)
            .IsUnique();

        builder.HasIndex(review => new { review.TransporterProfileId, review.IsHidden, review.CreatedAtUtc })
            .HasDatabaseName("idx_review_transporter");

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(review => review.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TransporterProfile>()
            .WithMany()
            .HasForeignKey(review => review.TransporterProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
