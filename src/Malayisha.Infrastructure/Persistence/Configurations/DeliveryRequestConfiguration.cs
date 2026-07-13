using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Malayisha.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryRequestConfiguration : IEntityTypeConfiguration<DeliveryRequest>
{
    public void Configure(EntityTypeBuilder<DeliveryRequest> builder)
    {
        builder.ToTable("delivery_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.OriginCity)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(request => request.DestinationCity)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(request => request.RequiredDateUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(request => request.WeightKg)
            .HasWeightPrecision()
            .IsRequired();

        builder.Property(request => request.SizeDescription)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(request => request.GoodsDescription)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(request => request.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(request => request.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
