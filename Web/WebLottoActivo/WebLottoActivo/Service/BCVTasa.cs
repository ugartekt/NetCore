using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLottoActivo.DBContext;
using WebLottoActivo.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebLottoActivo.Service
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

        public async Task<List<TasaBCV>> ListTasaUSDAsync()
        {
            List<TasaBCV> listTasaBCB = new List<TasaBCV>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                listTasaBCB = await db.tasaBCV.Where(t => t.symbol == "USD").OrderByDescending(t => t.id).ToListAsync();

                return listTasaBCB ?? new List<TasaBCV>(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<TasaBCV>(); // Devuelve objeto vacío en caso de error
            }
        }
        public async Task<List<TasaBCV>> ListTasaEURAsync()
        {
            List<TasaBCV> listTasaBCB = new List<TasaBCV>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                listTasaBCB = await db.tasaBCV.Where(t => t.symbol == "EUR").OrderByDescending(t => t.id).ToListAsync();

                return listTasaBCB ?? new List<TasaBCV>(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<TasaBCV>(); // Devuelve objeto vacío en caso de error
            }
        }

        public async Task<List<TasaBCV>> ListTasaCurrentAsync()
        {
            List<TasaBCV> listTasaBCB = new List<TasaBCV>();
            try
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                listTasaBCB = await db.tasaBCV.Where(t => t.date == date).OrderByDescending(t => t.id).ToListAsync();

                return listTasaBCB ?? new List<TasaBCV>(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<TasaBCV>(); // Devuelve objeto vacío en caso de error
            }
        }
    }
}
