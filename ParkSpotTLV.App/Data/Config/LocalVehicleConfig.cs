using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalVehicleConfig : IEntityTypeConfiguration<LocalVehicle>
{
    public void Configure(EntityTypeBuilder<LocalVehicle> e)
    {
        e.ToTable("local_vehicles");

        e.HasKey(x => x.Id);

        e.Property(x => x.Type).IsRequired();

        e.HasOne(v => v.Owner)
         .WithMany(u => u.Vehicles)
         .HasForeignKey(v => v.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // Local cache properties
        e.Property(x => x.CachedAt).IsRequired();
        e.Property(x => x.IsActive).HasDefaultValue(true);

        e.HasIndex(x => x.UserId);
        e.HasIndex(x => x.IsActive);
        e.HasIndex(x => x.CachedAt);
    }
}