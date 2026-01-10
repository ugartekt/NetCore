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
using WorkerServicesLocal.Interfaces;
using WorkerServicesLocal.Models;

namespace WorkerServicesLocal.Service
{
    public class LottoActivo : Interfaces.ILottoActivo
    {
        public string MSJ = string.Empty;

        private readonly IServiceScopeFactory _scopeFactory;

        public LottoActivo(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> InsertResultadoAsync(LottoActivoResultado lottoActivoResultado)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.lottoActivoResultados.AddAsync(lottoActivoResultado);
                await db.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return false;
            }

        }

        public async Task<LottoActivoResultado> UltimoAnimalitoDesplazamientoAsync()
        {
            LottoActivoResultado lottoActivoResultado = null;
            try
            {

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                lottoActivoResultado = await db.lottoActivoResultados
                                                                .OrderByDescending(t => t.id)
                                                                .FirstOrDefaultAsync();


                return lottoActivoResultado; // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return lottoActivoResultado; // Devuelve objeto vacío en caso de error
            }
        }

        public async Task<LottoActivoResultado> SearchResultadosAsync(string date, string hora)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var lottoActivoResultados = await db.lottoActivoResultados
                    .FirstOrDefaultAsync(t => t.fecha == date && t.hora == hora);

                return lottoActivoResultados ?? new LottoActivoResultado(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                MSJ = $"HA OCURRIDO UN ERROR BD: {Worker.GetFullExceptionDetails(ex)} FECHA: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                string rutaArchivo = "C:\\WorkerLocal\\ERROR\\Error_Busc.txt";

                // Abrir el archivo en modo de adición y escribir el texto
                using (StreamWriter sw = File.AppendText(rutaArchivo))
                {
                    sw.WriteLine(MSJ);
                }

                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new LottoActivoResultado(); // Devuelve objeto vacío en caso de error
            }
        }
    }
}
