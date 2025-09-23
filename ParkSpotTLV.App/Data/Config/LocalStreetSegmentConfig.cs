using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalStreetSegmentConfig : IEntityTypeConfiguration<LocalStreetSegment>
{
    public void Configure(EntityTypeBuilder<LocalStreetSegment> e)
    {
        e.ToTable("local_street_segments");

        e.HasKey(x => x.Id);

        e.Property(x => x.Name)
         .HasMaxLength(128);

        // Store geometry as JSON string in SQLite
        e.Property(x => x.GeometryJson)
         .IsRequired()
         .HasColumnType("TEXT");

        e.Property(x => x.ParkingType).IsRequired();
        e.Property(x => x.ParkingHours).IsRequired();
        e.Property(x => x.Side).IsRequired();

        e.HasOne(x => x.Zone)
         .WithMany(z => z.Segments)
         .HasForeignKey(x => x.ZoneId)
         .OnDelete(DeleteBehavior.SetNull);

        // Local cache properties
        e.Property(x => x.CachedAt).IsRequired();
        e.Property(x => x.IsActive).HasDefaultValue(true);

        e.HasIndex(x => x.ZoneId);
        e.HasIndex(x => x.IsActive);
        e.HasIndex(x => x.CachedAt);
    }
}