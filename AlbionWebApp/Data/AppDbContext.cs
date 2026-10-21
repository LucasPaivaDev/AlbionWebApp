using AlbionWebApp.Enums;
using AlbionWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AlbionWebApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ItemLabel> ItemLabels { get; set; }
    public DbSet<ItemPrice> ItemPrices { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Quality> Qualities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ids fixos, alinhados 1:1 com AlbionCityEnum/ItemQualityEnum — ItemPrice.CityId
        // e ItemPrice.QualityId gravam o valor numérico do enum diretamente (sem lookup),
        // então essas linhas precisam existir com esses Ids exatos antes de qualquer
        // ItemPrice ser persistido.
        modelBuilder.Entity<City>().HasData(
            new City { Id = (int)AlbionCityEnum.Caerleon, Name = nameof(AlbionCityEnum.Caerleon) },
            new City { Id = (int)AlbionCityEnum.Bridgewatch, Name = nameof(AlbionCityEnum.Bridgewatch) },
            new City { Id = (int)AlbionCityEnum.Lymhurst, Name = nameof(AlbionCityEnum.Lymhurst) },
            new City { Id = (int)AlbionCityEnum.Martlock, Name = nameof(AlbionCityEnum.Martlock) },
            new City { Id = (int)AlbionCityEnum.Thetford, Name = nameof(AlbionCityEnum.Thetford) },
            new City { Id = (int)AlbionCityEnum.FortSterling, Name = nameof(AlbionCityEnum.FortSterling) },
            new City { Id = (int)AlbionCityEnum.Brecilien, Name = nameof(AlbionCityEnum.Brecilien) }
        );

        modelBuilder.Entity<Quality>().HasData(
            new Quality { Id = (int)ItemQualityEnum.Normal, Name = nameof(ItemQualityEnum.Normal) },
            new Quality { Id = (int)ItemQualityEnum.Good, Name = nameof(ItemQualityEnum.Good) },
            new Quality { Id = (int)ItemQualityEnum.Outstanding, Name = nameof(ItemQualityEnum.Outstanding) },
            new Quality { Id = (int)ItemQualityEnum.Excellent, Name = nameof(ItemQualityEnum.Excellent) },
            new Quality { Id = (int)ItemQualityEnum.Masterpiece, Name = nameof(ItemQualityEnum.Masterpiece) }
        );
    }
}
