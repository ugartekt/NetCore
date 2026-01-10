using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebLottoActivo.DBContext;
using WebLottoActivo.Models;
using WebLottoActivo.Models.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebLottoActivo.Service
{
    public class LottoActivo : Interfaces.ILottoActivo
    {
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
        public async Task<List<LottoActivoAnimal>> ListLottoAnimal()
        {
            List<LottoActivoAnimal> listLottoActivoAnimal = new List<LottoActivoAnimal>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                listLottoActivoAnimal = await db.lottoActivoAnimals.OrderBy(x => x.id).ToListAsync();


                return listLottoActivoAnimal ?? new List<LottoActivoAnimal>(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<LottoActivoAnimal>(); // Devuelve objeto vacío en caso de error
            }
        }

        public async Task<List<DesplazamientoResumen>> ListCantidadDesplazamientoAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var sql = @"
                            SELECT
                                base.desplazamiento AS desplazamiento,
                                GROUP_CONCAT(posterior.desplazamiento, ',') AS desplazamientosPosteriores,
                                COUNT(posterior.desplazamiento) AS cantidad,
                                MAX(posterior.fecha) AS fecha
                            FROM LottoActivoResultado AS base
                            JOIN LottoActivoResultado AS posterior
                                ON posterior.fecha > base.fecha
                                OR (posterior.fecha = base.fecha AND posterior.hora > base.hora)
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM LottoActivoResultado AS intermedio
                                WHERE
                                    (intermedio.fecha > base.fecha OR (intermedio.fecha = base.fecha AND intermedio.hora > base.hora))
                                    AND (intermedio.fecha < posterior.fecha OR (intermedio.fecha = posterior.fecha AND intermedio.hora < posterior.hora))
                                    AND intermedio.desplazamiento != base.desplazamiento
                            )
                            GROUP BY base.desplazamiento
                            ORDER BY base.desplazamiento;
                        ";

                var result = await db.desplazamientoResumen.FromSqlRaw(sql).ToListAsync();
                return result ?? new List<DesplazamientoResumen>();
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<DesplazamientoResumen>();
            }
        }

        public async Task<List<DesplazamientoDiario>> ListDesplazamientoDiarioAsync( string date)
        {
            List<DesplazamientoDiario> listDesplazamientoDiario = new List<DesplazamientoDiario>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                listDesplazamientoDiario = await db.lottoActivoResultados
                                                .Where(r => r.fecha == date)
                                                .OrderBy(r => r.id)
                                                .Select(r => new DesplazamientoDiario
                                                {
                                                    Nombre = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.nombre : null,
                                                    ImageB64 = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.image : null,
                                                    Desplazamiento = r.desplazamiento,
                                                    Hora = r.hora
                                                })
                                                .ToListAsync();

                return listDesplazamientoDiario ?? new List<DesplazamientoDiario>(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<DesplazamientoDiario>(); // Devuelve objeto vacío en caso de error
            }
        }

        public async Task<List<DesplazamientoResumen>> ListDesplazamientoSeguidoAsync()
        {
            List<DesplazamientoResumen> listDesplazamientoResumen = new List<DesplazamientoResumen>();
            try
            {
                string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                string dateOld = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd");
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                listDesplazamientoResumen = await db.lottoActivoResultados
                                            .Where(r => r.fecha.CompareTo(dateOld) >= 0 && r.fecha.CompareTo(date) <= 0)
                                            .GroupBy(r => r.desplazamiento)
                                            .Where(g => g.Count() >= 2)
                                            .Select(g => new DesplazamientoResumen
                                            {
                                                Desplazamiento = g.Key,
                                                Cantidad = g.Count()
                                            })
                                            .OrderByDescending(x => x.Cantidad)
                                            .ToListAsync();


                return listDesplazamientoResumen ?? new List<DesplazamientoResumen>(); // Devuelve objeto vacío si no se encuentra
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<DesplazamientoResumen>(); // Devuelve objeto vacío en caso de error
            }
        }

        public async Task<List<CantidadTotalAnimalitos>> TotalHistorialAnimalito()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var resultado = await db.lottoActivoAnimals
                                    .GroupJoin(
                                        db.lottoActivoResultados,
                                        animal => animal.id,
                                        resultado => resultado.lottoActivoAnimalId,
                                        (animal, resultados) => new { animal, resultados }
                                    )
                                    .Select(grupo => new CantidadTotalAnimalitos
                                    {
                                        Id = (int)grupo.animal.id,
                                        Nombre = grupo.animal.nombre,
                                        ImageB64 = grupo.animal.image,
                                        Cantidad = grupo.resultados.Count(),
                                        UltimaFecha = grupo.resultados.Max(r => r.fecha)
                                    })
                                    .OrderByDescending(x => x.Cantidad)
                                    .ToListAsync();

                return resultado ?? new List<CantidadTotalAnimalitos>();
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<CantidadTotalAnimalitos>();
            }
        }



    }
}
