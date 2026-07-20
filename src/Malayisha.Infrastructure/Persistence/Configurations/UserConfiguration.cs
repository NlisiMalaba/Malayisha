using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.PushDeviceToken)
            .HasMaxLength(512);

        builder.Property(user => user.MarketingNotificationsOptIn)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(user => user.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique();
    }
}
