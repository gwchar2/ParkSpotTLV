using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class PermitConfig : IEntityTypeConfiguration<Permit> {
        public void Configure(EntityTypeBuilder<Permit> e) {
            e.ToTable("permits");

            e.HasKey(x => x.Id);

            e.Property(x => x.Type).IsRequired();

            e.Property(x => x.ZoneCode).IsRequired(false);
            e.HasIndex(x => x.ZoneCode);

            e.HasOne(x => x.Zone)
             .WithMany()
             .HasForeignKey(p => p.ZoneCode)
             .HasPrincipalKey(z => z.Code)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.VehicleId);
            e.HasOne(x => x.Vehicle)
             .WithMany(v => v.Permits)
             .HasForeignKey(p => p.VehicleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(v => v.Xmin)
             .IsConcurrencyToken()
             .ValueGeneratedOnAddOrUpdate()
             .HasColumnName("xmin");

            e.Property(x => x.LastUpdated);
        }
    }
}