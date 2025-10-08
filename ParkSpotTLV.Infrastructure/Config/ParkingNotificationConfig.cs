using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class ParkingNotificationConfig : IEntityTypeConfiguration<ParkingNotification> {

        public void Configure (EntityTypeBuilder<ParkingNotification> e) {

            e.ToTable("parking_notifications");
            e.HasKey(x => x.Id);

            e.Property(x => x.NotificationMinutes).IsRequired();

            e.Property(x => x.CreatedAt).IsRequired();

            e.HasIndex(x => new {
                x.NotifyAt,
                x.IsSent
            });

            e.HasOne<ParkingSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();

            e.Property(x => x.JobId).HasMaxLength(256);
            e.Property(x => x.AttemptCount).HasDefaultValue(0);
            e.Property(x => x.LastAttemptAt);
            e.Property(x => x.ProviderMessageId).HasMaxLength(256);
            e.Property(x => x.Error);
            e.HasIndex(x => new { 
                x.NotifyAt, 
                x.Status 
            }).HasDatabaseName("IX_notification_notify_at_status");

        }

    }
}
