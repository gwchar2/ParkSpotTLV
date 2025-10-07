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
        }

    }
}
