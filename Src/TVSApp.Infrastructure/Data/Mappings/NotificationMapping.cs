using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TVS_App.Domain.Entities;

namespace TVS_App.Infrastructure.Data.Mappings;

public class NotificationMapping : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .IsRequired();
    }
}