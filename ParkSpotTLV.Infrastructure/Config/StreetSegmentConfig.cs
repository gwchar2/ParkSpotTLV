using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class StreetSegmentConfig : IEntityTypeConfiguration<StreetSegment>{

        public void Configure(EntityTypeBuilder<StreetSegment> e) {

            e.ToTable("street_segments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ZoneId).HasDatabaseName("ix_segments_zone_id");

            e.Property(x => x.Name).HasMaxLength(128);

            e.Property(x => x.Geom)
                .HasColumnType("geometry(MultiLineString,4326)")
                .IsRequired();
            e.HasIndex(x => x.Geom).HasMethod("gist").HasDatabaseName("gist_segments_geometry");

            e.HasOne(x => x.Zone)
                .WithMany(z => z.Segments)
                .HasForeignKey(x => x.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.CarsOnly);

            e.Property(x => x.ParkingType).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.ParkingHours).HasConversion<string>().HasMaxLength(16);

        }
    }
}
