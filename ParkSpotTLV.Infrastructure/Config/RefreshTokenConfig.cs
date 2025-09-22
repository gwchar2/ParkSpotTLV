using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken> {

        public void Configure(EntityTypeBuilder<RefreshToken> e) {

            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);

            e.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            e.Property(x => x.ReplacedByTokenHash)
                .HasMaxLength(64);

            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.ExpiresAtUtc);
            e.HasIndex(x => x.RevokedAtUtc);
            e.HasIndex(x => x.ReplacedByTokenHash);

            e.HasOne(x => x.User)
                .WithMany(g => g.RefreshToken)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
