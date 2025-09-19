using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class PermitConfig : IEntityTypeConfiguration<Permit> {
        public void Configure(EntityTypeBuilder<Permit> e) {
            e.ToTable("permits");

            e.HasKey(x => x.Id);

            e.Property(x => x.ValidTo);

            e.Property(x => x.IsActive).HasDefaultValue(true);

            e.HasOne(x => x.Zone)
             .WithMany()
             .HasForeignKey(x => x.ZoneId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.Permits)
             .HasForeignKey(x => x.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.VehicleId, x.ZoneId, x.Type });
        }
    }
}