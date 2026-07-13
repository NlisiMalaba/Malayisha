using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class OtpRecordConfiguration : IEntityTypeConfiguration<OtpRecord>
{
    public void Configure(EntityTypeBuilder<OtpRecord> builder)
    {
        builder.ToTable("otp_records");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(record => record.OtpHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(record => record.IssuedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(record => record.ExpiresAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(record => record.PhoneNumber);
    }
}
