using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class PendingNotificationConfiguration : IEntityTypeConfiguration<PendingNotification>
{
    public void Configure(EntityTypeBuilder<PendingNotification> builder)
    {
        builder.ToTable("pending_notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.DeviceToken)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(notification => notification.DataJson)
            .HasColumnType("jsonb");

        builder.Property(notification => notification.LastError)
            .HasMaxLength(2000);

        builder.Property(notification => notification.NextRetryAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(notification => notification.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(notification => notification.LastAttemptAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(notification => notification.NextRetryAtUtc)
            .HasDatabaseName("idx_pending_notifications_next_retry");
    }
}
