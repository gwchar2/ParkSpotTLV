using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalPermitConfig : IEntityTypeConfiguration<LocalPermit>
{
    public void Configure(EntityTypeBuilder<LocalPermit> e)
    {
        e.ToTable("local_permits");

        e.HasKey(x => x.Id);

        e.Property(x => x.Type).IsRequired();
        e.Property(x => x.ValidTo);
        e.Property(x => x.IsActive).HasDefaultValue(true);

        e.Property(x => x.ZoneCode).IsRequired(false);
        e.HasIndex(x => x.ZoneCode);

        e.HasOne(x => x.Zone)
          .WithMany()
          .HasForeignKey(x => x.ZoneId)
          .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(x => x.Vehicle)
         .WithMany(v => v.Permits)
         .HasForeignKey(x => x.VehicleId)
         .OnDelete(DeleteBehavior.Cascade);

        // Local cache properties
        e.Property(x => x.CachedAt).IsRequired();

        e.HasIndex(x => x.VehicleId);
        e.HasIndex(x => x.ZoneId);
        e.HasIndex(x => x.IsActive);
        e.HasIndex(x => x.CachedAt);
    }
}