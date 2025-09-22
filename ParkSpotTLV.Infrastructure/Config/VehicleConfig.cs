using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class VehicleConfig : IEntityTypeConfiguration<Vehicle> {
        public void Configure(EntityTypeBuilder<Vehicle> e) {
            e.ToTable("vehicles");

            e.HasKey(x => x.Id);

            e.HasOne(v => v.Owner)
             .WithMany(u => u.Vehicles)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}