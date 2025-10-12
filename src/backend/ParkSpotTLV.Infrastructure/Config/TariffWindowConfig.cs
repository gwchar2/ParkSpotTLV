using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure.Config {
    public sealed class TariffWindowConfig : IEntityTypeConfiguration<TariffWindow>{

        public void Configure (EntityTypeBuilder<TariffWindow> e) {

            e.ToTable("tariff_windows");
            e.HasKey(x => x.Id);

            e.Property(x => x.Tariff).IsRequired();
            e.Property(x => x.DayOfWeek).IsRequired();
            e.Property(x => x.StartLocal).HasColumnType("time").IsRequired();
            e.Property(x => x.EndLocal).HasColumnType("time").IsRequired();

            e.HasIndex(x => new {
                x.Tariff,
                x.DayOfWeek
            });
        }


    }
}
