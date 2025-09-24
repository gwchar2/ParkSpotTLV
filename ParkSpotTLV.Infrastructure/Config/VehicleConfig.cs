using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class VehicleConfig : IEntityTypeConfiguration<Vehicle> {
        public void Configure(EntityTypeBuilder<Vehicle> e) {
            e.ToTable("vehicles");

            e.HasKey(x => x.Id);

            e.Property(x => x.OwnerId).IsRequired();
            e.HasIndex(x => x.OwnerId);

            e.HasOne(v => v.Owner)
             .WithMany(u => u.Vehicles)
             .HasForeignKey(x => x.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(x => x.Type).IsRequired();

            e.HasMany(v => v.Permits)
             .WithOne(p => p.Vehicle)
             .HasForeignKey(p => p.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(v => v.Xmin)
             .IsConcurrencyToken()
             .ValueGeneratedOnAddOrUpdate()
             .HasColumnName("xmin");
        }
    }
}