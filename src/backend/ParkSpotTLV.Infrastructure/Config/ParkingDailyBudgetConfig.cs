using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {

    public sealed class ParkingDailyBudgetConfig : IEntityTypeConfiguration<ParkingDailyBudget> {
        public void Configure (EntityTypeBuilder<ParkingDailyBudget> e){

            e.ToTable("daily_budgets");

            e.HasKey(p => new {
                p.VehicleId,
                p.AnchorDate
            });

            e.Property(p => p.VehicleId).IsRequired();
            e.Property(p => p.AnchorDate).IsRequired();

            e.Property(p => p.MinutesUsed).IsRequired().HasDefaultValue(0);

            e.Property(p => p.CreatedAtUtc).IsRequired();
            e.Property(p => p.UpdatedAtUtc).IsRequired();

            e.HasOne<Vehicle>().WithMany().HasForeignKey(p => p.VehicleId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
