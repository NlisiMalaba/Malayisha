using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Text)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(message => message.SentAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(message => message.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
