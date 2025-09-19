using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class VehicleConfig : IEntityTypeConfiguration<Vehicle>{
        public void Configure(EntityTypeBuilder<Vehicle> e) {

            e.ToTable("vehicles");
            e.HasKey(x => x.Id);

            e.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(16);

            e.Property(x => x.HasDisabledPermit).IsRequired();

            e.Property(x => x.ZonePermit).HasDefaultValue(0);
            e.ToTable(t => t.HasCheckConstraint(
                "ck_vehicle_zone_permit",
                "\"ZonePermit\" >= 0 AND \"ZonePermit\" <= 10"));

            e.HasOne(x => x.Owner)
             .WithMany(u => u.Vehicles)
             .HasForeignKey(x => x.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}