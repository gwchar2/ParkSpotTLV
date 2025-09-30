using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {

    /*
     * Fluent mappings + indexes for window tables.
     * Npgsql supports TimeOnly → "time" natively; SRID/geometry unaffected.
     */
    public class TariffGroupWindowConfig : IEntityTypeConfiguration<TariffGroupWindow> {
        public void Configure(EntityTypeBuilder<TariffGroupWindow> b) {
            b.ToTable("tariff_group_windows");
            b.HasKey(x => x.Id);

            // Index by group, enable quick scans by Taarif + Enabled + Priority
            b.HasIndex(x => new { x.Taarif, x.Enabled, x.Priority });

            b.Property(x => x.Taarif).IsRequired();
            b.Property(x => x.Days).HasConversion<int>().IsRequired();

            b.Property(x => x.IsAllDay).HasDefaultValue(false);
            b.Property(x => x.StartLocalTime).HasColumnType("time");
            b.Property(x => x.EndLocalTime).HasColumnType("time");

            b.Property(x => x.Priority).HasDefaultValue(0);
            b.Property(x => x.Enabled).HasDefaultValue(true);
        }
    }

    public class StreetSegmentRuleWindowConfig : IEntityTypeConfiguration<StreetSegmentRuleWindow> {
        public void Configure(EntityTypeBuilder<StreetSegmentRuleWindow> b) {
            b.ToTable("street_segment_rule_windows");
            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.StreetSegmentId, x.Enabled, x.Priority });

            b.Property(x => x.Kind).IsRequired();
            b.Property(x => x.Days).HasConversion<int>().IsRequired();
            b.Property(x => x.IsAllDay).HasDefaultValue(false);
            b.Property(x => x.StartLocalTime).HasColumnType("time");
            b.Property(x => x.EndLocalTime).HasColumnType("time");

            b.Property(x => x.AppliesToSide).IsRequired();
            b.Property(x => x.Priority).HasDefaultValue(100);
            b.Property(x => x.Enabled).HasDefaultValue(true);

            b.HasOne(x => x.StreetSegment)
             .WithMany() // keep overrides independent; you can add a nav on StreetSegment if you want
             .HasForeignKey(x => x.StreetSegmentId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}