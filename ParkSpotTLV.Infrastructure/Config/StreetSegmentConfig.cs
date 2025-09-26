using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class StreetSegmentConfig : IEntityTypeConfiguration<StreetSegment> {
        public void Configure(EntityTypeBuilder<StreetSegment> e) {
            e.ToTable("street_segments");

            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OSMId);

            e.Property(x => x.NameEnglish)
             .HasMaxLength(128);
            e.Property(x => x.NameHebrew)
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

