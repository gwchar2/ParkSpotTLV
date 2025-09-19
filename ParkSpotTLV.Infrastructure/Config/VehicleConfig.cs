using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class VehicleConfig : IEntityTypeConfiguration<Vehicle> {
        public void Configure(EntityTypeBuilder<Vehicle> e) {
            e.ToTable("vehicles");

            e.HasKey(x => x.Id);

            e.Property(x => x.PlateNumber).HasMaxLength(20);

            e.HasOne(v => v.Owner)
             .WithMany(u => u.Vehicles)
             .HasForeignKey(v => v.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.PlateNumber).IsUnique();
        }
    }
}