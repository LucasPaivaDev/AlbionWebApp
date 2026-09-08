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
    }
}
