using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class ZoneConfig : IEntityTypeConfiguration<Zone> {
        public void Configure(EntityTypeBuilder<Zone> e) {
            e.ToTable("zones");
            e.HasKey(z => z.Id);


            e.Property(z => z.Name).HasMaxLength(16);

            e.Property(z => z.Taarif).IsRequired();

            e.Property(z => z.Code).IsRequired();
            e.HasAlternateKey(z => z.Code);
            e.HasIndex(z => z.Code).IsUnique();

            e.Property(z => z.Geom)
             .IsRequired()
             .HasColumnType("geometry(MultiPolygon,4326)");

            e.HasIndex(z => z.Geom).HasMethod("GIST");

        }
    }
}