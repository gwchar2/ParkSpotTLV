using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;



namespace ParkSpotTLV.Infrastructure.Config {
    public sealed class DevicePushTokenConfig : IEntityTypeConfiguration<DevicePushToken> {

        public void Configure (EntityTypeBuilder<DevicePushToken> e) {

            e.ToTable("device_push_tokens");
            e.HasKey(x => x.Id);

            e.Property(x => x.UserId).IsRequired();

            e.Property(x => x.Platform).HasMaxLength(20).IsRequired();

            e.Property(x => x.Token).IsRequired();      // Really long, kind of pointless to put max length 4000+++

            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.LastSeen).IsRequired();

            e.Property(x => x.IsRevoked).HasDefaultValue(false);

            e.HasIndex(x => x.UserId).HasDatabaseName("IX_DevicePushToken_UserId");
            e.HasIndex(x => x.Token).IsUnique().HasDatabaseName("UX_DevicePushToken_Token");
        }
    }
}
