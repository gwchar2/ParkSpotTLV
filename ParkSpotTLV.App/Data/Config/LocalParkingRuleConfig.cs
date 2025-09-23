using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalParkingRuleConfig : IEntityTypeConfiguration<LocalParkingRule>
{
    public void Configure(EntityTypeBuilder<LocalParkingRule> e)
    {
        e.ToTable("local_parking_rules");

        e.HasKey(x => x.Id);

        e.Property(x => x.DayOfWeek).IsRequired();
        e.Property(x => x.StartTime).IsRequired();
        e.Property(x => x.EndTime).IsRequired();
        e.Property(x => x.StylePriority).HasDefaultValue(2);
        e.Property(x => x.ParkingType).IsRequired();
        e.Property(x => x.MaxDurationMinutes).HasDefaultValue(-1);

        e.Property(x => x.Note)
         .HasMaxLength(256);

        e.HasOne(x => x.StreetSegment)
         .WithMany(s => s.ParkingRules)
         .HasForeignKey(x => x.StreetSegmentId)
         .OnDelete(DeleteBehavior.Cascade);

        // Local cache properties
        e.Property(x => x.CachedAt).IsRequired();
        e.Property(x => x.IsActive).HasDefaultValue(true);

        e.HasIndex(x => x.StreetSegmentId);
        e.HasIndex(x => x.DayOfWeek);
        e.HasIndex(x => x.StylePriority);
        e.HasIndex(x => x.IsActive);
        e.HasIndex(x => x.CachedAt);
    }
}