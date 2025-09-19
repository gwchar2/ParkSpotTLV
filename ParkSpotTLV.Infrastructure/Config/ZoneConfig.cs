using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class ZoneConfig : IEntityTypeConfiguration<Zone> {
        public void Configure(EntityTypeBuilder<Zone> e) {
            e.ToTable("zones");
            e.HasKey(x => x.Id);

            e.Property(x => x.Geom)
                .HasColumnType("geometry(MultiPolygon,4326)")
                .IsRequired();

            e.HasIndex(x => x.Geom)
             .HasMethod("gist")
             .HasDatabaseName("gist_zones_geometry");

            e.HasIndex(x => x.ZonePermit)
                .IsUnique()
                .HasDatabaseName("ux_zones_zone_permit");
        }
    }
}
