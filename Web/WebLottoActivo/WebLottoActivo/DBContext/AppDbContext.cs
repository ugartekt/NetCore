using Microsoft.EntityFrameworkCore;
using WebLottoActivo.Models;
using WebLottoActivo.Models.ViewModels;

namespace WebLottoActivo.DBContext
{
    public class AppDbContext : DbContext
    {
        public DbSet<TasaBCV> tasaBCV { get; set; }
        public DbSet<LottoActivoAnimal> lottoActivoAnimals { get; set; }
        public DbSet<LottoActivoResultado> lottoActivoResultados { get; set; }

        public DbSet<DesplazamientoResumen> desplazamientoResumen { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TasaBCV>().ToTable("TasaBCV");
            modelBuilder.Entity<LottoActivoAnimal>().ToTable("LottoActivoAnimal");
            modelBuilder.Entity<LottoActivoResultado>().ToTable("LottoActivoResultado");

            modelBuilder.Entity<DesplazamientoResumen>().HasNoKey();

        }
    }

}
