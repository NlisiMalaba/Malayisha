using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class VerificationConfiguration : IEntityTypeConfiguration<Verification>
{
    public void Configure(EntityTypeBuilder<Verification> builder)
    {
        builder.ToTable("verifications");

        builder.HasKey(verification => verification.Id);

        builder.Property(verification => verification.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(verification => verification.SubmittedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(verification => verification.ReviewedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(verification => verification.RejectionReason)
            .HasMaxLength(1000);

        builder.HasOne<TransporterProfile>()
            .WithMany()
            .HasForeignKey(verification => verification.TransporterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(verification => verification.ReviewedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
