using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class StreetSegmentConfig : IEntityTypeConfiguration<StreetSegment> {
        public void Configure(EntityTypeBuilder<StreetSegment> e) {
            e.ToTable("street_segments");

            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
             .HasMaxLength(128);

            e.Property(x => x.Geom)
             .IsRequired()
             .HasColumnType("geometry(LineString,4326)");

            e.HasIndex(x => x.Geom).HasMethod("GIST");

            e.HasOne(x => x.Zone)
             .WithMany(z => z.Segments)
             .HasForeignKey(x => x.ZoneId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.ZoneId);
        }
    }
}
