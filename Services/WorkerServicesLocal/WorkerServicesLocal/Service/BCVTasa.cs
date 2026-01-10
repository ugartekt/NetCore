using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerServicesLocal.DBContext;
using WorkerServicesLocal.Models;

namespace WorkerServicesLocal.Service
{
    public class BCVTasa : Interfaces.IBCVTasa
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BCVTasa(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> InsertTasaAsync(TasaBCV tasaBCV)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.tasaBCV.AddAsync(tasaBCV);
                await db.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return false;
            }

        }

        public async Task<TasaBCV> SearchTasaAsync(string date)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var tasaBCV = await db.tasaBCV
                    .FirstOrDefaultAsync(t => t.date == date);

                return tasaBCV ?? new TasaBCV(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new TasaBCV(); // Devuelve objeto vacío en caso de error
            }
        }
    }
}
