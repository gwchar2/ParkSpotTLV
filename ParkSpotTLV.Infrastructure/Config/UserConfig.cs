using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class UserConfig : IEntityTypeConfiguration<User> {
        public void Configure(EntityTypeBuilder<User> e) {
            e.ToTable("users");

            e.HasKey(x => x.Id);

            e.Property(x => x.Username)
             .IsRequired()
             .HasMaxLength(64);
            e.HasIndex(x => x.Username).IsUnique();

            e.Property(x => x.PasswordHash)
             .IsRequired()
             .HasMaxLength(256);

            e.Property(x => x.ParkingStartedAtUtc);

            e.Property(x => x.FreeParkingUntilUtc)              
             .HasColumnType("timestamptz");

            e.Property(x => x.FreeParkingBudget)                
             .HasColumnType("interval")
             .HasDefaultValueSql("interval '2 hours'");

            e.Property(x => x.LastUpdated);

        }
    }
}
