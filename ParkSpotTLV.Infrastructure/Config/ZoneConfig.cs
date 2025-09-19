using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class ZoneConfig : IEntityTypeConfiguration<Zone> {
        public void Configure(EntityTypeBuilder<Zone> e) {
            e.ToTable("zones");

            e.HasKey(x => x.Id);

            e.Property(x => x.Name).HasMaxLength(64);

            // Geometry: MultiPolygon with SRID 4326
            e.Property(x => x.Geom)
             .IsRequired()
             .HasColumnType("geometry(MultiPolygon,4326)");

            e.HasIndex(x => x.Geom).HasMethod("GIST");

            e.HasIndex(x => x.Code);
        }
    }
}