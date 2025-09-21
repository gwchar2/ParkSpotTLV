using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class ParkingRuleConfig : IEntityTypeConfiguration<ParkingRule> {
        public void Configure(EntityTypeBuilder<ParkingRule> e) {
            e.ToTable("parking_rules");

            e.HasKey(x => x.Id);

            e.Property(x => x.Note).HasMaxLength(256);

            e.Property(x => x.StartTime).IsRequired();
            e.Property(x => x.EndTime).IsRequired();

            // Optional default for MaxDurationMinutes (-1 = no limit)
            e.Property(x => x.MaxDurationMinutes).HasDefaultValue(-1);

            e.HasOne(x => x.StreetSegment)
             .WithMany(s => s.ParkingRules)
             .HasForeignKey(x => x.StreetSegmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.ToTable(t => t.HasCheckConstraint("ck_parkingrule_dayofweek_range", "\"DayOfWeek\" BETWEEN 0 AND 6"));
            e.ToTable(t => t.HasCheckConstraint("ck_parkingrule_time_order", "\"StartTime\" < \"EndTime\""));

            e.HasIndex(x => x.StylePriority);
        }
    }
}