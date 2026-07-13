using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class TransporterProfileConfiguration : IEntityTypeConfiguration<TransporterProfile>
{
    public void Configure(EntityTypeBuilder<TransporterProfile> builder)
    {
        builder.ToTable("transporter_profiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(profile => profile.VehicleDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(profile => profile.CapacityKg)
            .HasWeightPrecision()
            .IsRequired();

        builder.Property(profile => profile.ProfilePhotoUrl)
            .HasMaxLength(2048);

        builder.Property(profile => profile.AverageRating)
            .HasPrecision(3, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(profile => profile.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(profile => profile.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(profile => profile.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
