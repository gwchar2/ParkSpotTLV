using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalUserConfig : IEntityTypeConfiguration<LocalUser>
{
    public void Configure(EntityTypeBuilder<LocalUser> e)
    {
        e.ToTable("local_users");

        e.HasKey(x => x.Id);

        e.Property(x => x.Username)
         .IsRequired()
         .HasMaxLength(64);
        e.HasIndex(x => x.Username).IsUnique();

        e.Property(x => x.PasswordHash)
         .IsRequired()
         .HasMaxLength(256);

        // Local-only properties
        e.Property(x => x.AuthToken)
         .HasMaxLength(500);

        e.Property(x => x.IsLoggedIn)
         .HasDefaultValue(false);

        e.Property(x => x.IsActive)
         .HasDefaultValue(true);

        e.Property(x => x.CachedAt)
         .IsRequired();

        e.Property(x => x.LastSyncAt)
         .IsRequired();
    }
}