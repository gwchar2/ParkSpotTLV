using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Config;

public class LocalRefreshTokenConfig : IEntityTypeConfiguration<LocalRefreshToken>
{
    public void Configure(EntityTypeBuilder<LocalRefreshToken> e)
    {
        e.ToTable("local_refresh_tokens");
        e.HasKey(x => x.Id);

        e.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(500); // Adjusted for local storage

        e.Property(x => x.ReplacedByTokenHash)
            .HasMaxLength(500);

        e.Property(x => x.CreatedAtUtc).IsRequired();
        e.Property(x => x.ExpiresAtUtc).IsRequired();

        e.HasIndex(x => x.TokenHash).IsUnique();
        e.HasIndex(x => x.UserId);
        e.HasIndex(x => x.ExpiresAtUtc);
        e.HasIndex(x => x.RevokedAtUtc);
        e.HasIndex(x => x.ReplacedByTokenHash);

        e.HasOne(x => x.User)
            .WithMany(g => g.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Local cache properties
        e.Property(x => x.CachedAt).IsRequired();
        e.Property(x => x.IsActive).HasDefaultValue(true);

        e.HasIndex(x => x.IsActive);
        e.HasIndex(x => x.CachedAt);
    }
}