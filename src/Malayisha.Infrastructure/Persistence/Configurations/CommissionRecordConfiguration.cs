using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class CommissionRecordConfiguration : IEntityTypeConfiguration<CommissionRecord>
{
    public void Configure(EntityTypeBuilder<CommissionRecord> builder)
    {
        builder.ToTable("commission_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.AgreedPriceZar)
            .HasMoneyPrecision()
            .IsRequired();

        builder.Property(record => record.CommissionRate)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(record => record.CommissionAmountZar)
            .HasMoneyPrecision()
            .IsRequired();

        builder.Property(record => record.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(record => record.CompletionDateUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(record => record.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(record => record.BookingId)
            .IsUnique();

        builder.HasIndex(record => new { record.Status, record.CompletionDateUtc })
            .HasDatabaseName("idx_commission_status_date");

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(record => record.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(record => record.TransporterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(record => record.UpdatedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
