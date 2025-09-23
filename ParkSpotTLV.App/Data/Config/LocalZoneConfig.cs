using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalZoneConfig : IEntityTypeConfiguration<LocalZone>
{
    public void Configure(EntityTypeBuilder<LocalZone> e)
    {
        e.ToTable("local_zones");
        e.HasKey(z => z.Id);

        e.Property(z => z.Name).HasMaxLength(64);

        e.Property(z => z.Code).IsRequired();
        e.HasAlternateKey(z => z.Code);
        e.HasIndex(z => z.Code).IsUnique();

        e.Property(z => z.Taarif).IsRequired();

        // Store geometry as JSON string in SQLite
        e.Property(z => z.GeometryJson)
         .IsRequired()
         .HasColumnType("TEXT");

        // Local cache properties
        e.Property(z => z.CachedAt).IsRequired();
        e.Property(z => z.IsActive).HasDefaultValue(true);

        e.HasIndex(z => z.IsActive);
        e.HasIndex(z => z.CachedAt);
    }
}