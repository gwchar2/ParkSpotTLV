using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public sealed class ParkingSessionConfig : IEntityTypeConfiguration<ParkingSession>{

        public void Configure(EntityTypeBuilder<ParkingSession> e) {

            e.ToTable("parking_sessions");
            e.HasKey(x => x.Id);


            e.Property(x => x.Group).HasMaxLength(16).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(128);
            e.Property(x => x.ParkingType).HasConversion<int>().IsRequired();
            e.Property(x => x.Tariff).HasConversion<int>().IsRequired();


            e.Property(x => x.ParkingBudgetUsed).HasDefaultValue(0).IsRequired();
            e.Property(x => x.PaidMinutes).HasDefaultValue(0).IsRequired();
            e.Property(x => x.Status).HasConversion<int>().IsRequired();

            // For one active session only
            e.HasIndex(x => x.VehicleId)
             .HasDatabaseName("IX_parking_sessions_vehicle_active")
             .HasFilter("\"stopped_utc\" IS NULL")
             .IsUnique();

            e.HasIndex(x => x.PlannedEndUtc)
             .HasDatabaseName("idx_ps_due")
             .HasFilter("\"stopped_utc\" IS NULL AND \"status\" = 1");


            e.HasIndex(x => x.StartedUtc);
            e.HasIndex(x => new {
                x.VehicleId,
                x.Status
            });

            e.ToTable(t => t.HasCheckConstraint(
                "ck_parking_sessions_parking_budget_used",
                "\"parking_budget_used\" >= 0"));

            e.ToTable(t => t.HasCheckConstraint(
                "ck_parking_sessions_paid_minutes",
                "\"paid_minutes\" >= 0"));


        }
    }
}



