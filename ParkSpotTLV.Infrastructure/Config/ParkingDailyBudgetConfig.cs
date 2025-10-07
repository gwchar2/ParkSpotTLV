using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {

    public sealed class ParkingDailyBudgetConfig : IEntityTypeConfiguration<ParkingDailyBudget> {
        public void Configure (EntityTypeBuilder<ParkingDailyBudget> e){

            e.ToTable("daily_budgets");

            e.HasKey(k => new {
                k.VehicleId,
                k.AnchorDate
            });

            e.Property(p => p.VehicleId).IsRequired();
            e.Property(p => p.AnchorDate).IsRequired();

            e.Property(p => p.MinutesUsed).IsRequired().HasDefaultValue(0);

            e.Property(p => p.CreatedAt).IsRequired();
            e.Property(p => p.UpdatedAt).IsRequired();

            e.HasOne<Vehicle>().WithMany().HasForeignKey(p => p.VehicleId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
