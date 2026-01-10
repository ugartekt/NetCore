using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkerServicesLocal.Models;

namespace WorkerServicesLocal.DBContext
{
    public class AppDbContext : DbContext
    {
        public DbSet<TasaBCV> tasaBCV { get; set; }
        public DbSet<LottoActivoAnimal> lottoActivoAnimals { get; set; }
        public DbSet<LottoActivoResultado> lottoActivoResultados { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TasaBCV>().ToTable("TasaBCV");
            modelBuilder.Entity<LottoActivoAnimal>().ToTable("LottoActivoAnimal");
            modelBuilder.Entity<LottoActivoResultado>().ToTable("LottoActivoResultado");
        }
    }
}
