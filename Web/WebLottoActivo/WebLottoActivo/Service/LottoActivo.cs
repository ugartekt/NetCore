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

        private static TimeSpan ParseTimeSafe(string hora)
        {
            if (string.IsNullOrEmpty(hora)) return TimeSpan.Zero;

            // Try parse common formats including 24h and 12h with AM/PM
            // Try DateTime parse first (handles "02:00PM", "2:00 PM", etc.)
            if (DateTime.TryParse(hora, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var dt))
            {
                return dt.TimeOfDay;
            }

            // try HH:mm[:ss] numeric split
            var parts = hora.Split(':');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                {
                    int s = 0;
                    if (parts.Length >= 3) int.TryParse(parts[2], out s);
                    return new TimeSpan(h, m, s);
                }
            }

            // fallback: try TimeSpan parse
            if (TimeSpan.TryParse(hora, out var ts)) return ts;
            return TimeSpan.Zero;
        }

        public LottoActivo(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<(int minYear, int maxYear)> GetAvailableYearRangeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Use aggregation to get min/max fecha and parse years
            var minFecha = await db.lottoActivoResultados.MinAsync(r => (string)r.fecha);
            var maxFecha = await db.lottoActivoResultados.MaxAsync(r => (string)r.fecha);
            if (string.IsNullOrEmpty(minFecha) || string.IsNullOrEmpty(maxFecha)) return (DateTime.Today.Year, DateTime.Today.Year);
            int minYear = DateTime.TryParse(minFecha, out var d1) ? d1.Year : DateTime.Today.Year;
            int maxYear = DateTime.TryParse(maxFecha, out var d2) ? d2.Year : DateTime.Today.Year;
            return (minYear, maxYear);
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
    // Additional comment for clarity
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

        public async Task<List<DesplazamientoResumen>> ListCantidadDesplazamientoAsync(int? year = null, int? month = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Optional filter by year/month (fecha stored as yyyy-MM-dd)
                string filterPrefix = null;
                if (year.HasValue && month.HasValue)
                {
                    filterPrefix = year.Value + "-" + month.Value.ToString("D2") + "-";
                }

                // Load filtered results ordered by fecha,hora,id
                var raw = await db.lottoActivoResultados
                                  .AsNoTracking()
                                  .Where(r => string.IsNullOrEmpty(filterPrefix) ? true : r.fecha.StartsWith(filterPrefix))
                                  .Select(r => new { r.id, r.fecha, r.hora, r.desplazamiento })
                                  .ToListAsync();

                // parse hora into TimeSpan safely and order in memory to ensure correct chronological ordering
                var all = raw.Select(r => new
                {
                    r.id,
                    r.fecha,
                    r.hora,
                    r.desplazamiento,
                    Time = ParseTimeSafe(r.hora)
                })
                .OrderBy(r => r.fecha)
                .ThenBy(r => r.Time)
                .ThenBy(r => r.id)
                .ToList();

                // Build map: baseDesplazamiento -> list of posterior desplazamientos in chronological order
                var map = new Dictionary<int, List<int>>();
                var mapCount = new Dictionary<int, int>();
                var mapMaxFecha = new Dictionary<int, string>();

                for (int i = 0; i < all.Count; i++)
                {
                    var baseRow = all[i];
                    int baseVal = baseRow.desplazamiento;
                    if (!map.ContainsKey(baseVal)) { map[baseVal] = new List<int>(); mapCount[baseVal] = 0; mapMaxFecha[baseVal] = null; }

                    // include subsequent records until and including first different desplazamiento encountered
                    for (int j = i + 1; j < all.Count; j++)
                    {
                        var posterior = all[j];
                        map[baseVal].Add(posterior.desplazamiento);
                        mapCount[baseVal]++;
                        mapMaxFecha[baseVal] = posterior.fecha; // later ones overwrite to keep max
                        if (posterior.desplazamiento != baseVal)
                        {
                            break; // stop for this baseRow
                        }
                    }
                }

                var result = map.Select(kv => new DesplazamientoResumen
                {
                    Desplazamiento = kv.Key,
                    DesplazamientosPosteriores = string.Join(",", kv.Value),
                    Cantidad = mapCount.ContainsKey(kv.Key) ? mapCount[kv.Key] : 0,
                    Fecha = mapMaxFecha.ContainsKey(kv.Key) ? mapMaxFecha[kv.Key] : null
                })
                .OrderBy(x => x.Desplazamiento)
                .ToList();

                return result;
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
                                                    LottoActivoAnimalId = r.lottoActivoAnimalId,
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

        public async Task<List<CantidadTotalAnimalitos>> TotalHistorialAnimalito(int? year = null, int? month = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Filter resultados by optional year/month
                var resultadosQuery = db.lottoActivoResultados.AsQueryable();
                if (year.HasValue && month.HasValue)
                {
                    string monthStr = month.Value.ToString("D2");
                    string prefix = year.Value + "-" + monthStr + "-";
                    resultadosQuery = resultadosQuery.Where(r => r.fecha.StartsWith(prefix));
                }

                var resultado = await db.lottoActivoAnimals
                                    .GroupJoin(
                                        resultadosQuery,
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

        public async Task<List<Models.ViewModels.AnimalitoResumen>> ListCantidadAnimalitoAsync(int? year = null, int? month = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Optional filter by year/month (fecha stored as yyyy-MM-dd)
                string filterPrefix = null;
                if (year.HasValue && month.HasValue)
                {
                    filterPrefix = year.Value + "-" + month.Value.ToString("D2") + "-";
                }

                var raw = await db.lottoActivoResultados
                                  .AsNoTracking()
                                  .Where(r => string.IsNullOrEmpty(filterPrefix) ? true : r.fecha.StartsWith(filterPrefix))
                                  .Select(r => new { r.id, r.fecha, r.hora, r.lottoActivoAnimalId, AnimalName = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.nombre : null, AnimalImage = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.image : null })
                                  .ToListAsync();

                var all = raw.Select(r => new
                {
                    r.id,
                    r.fecha,
                    r.hora,
                    AnimalId = r.lottoActivoAnimalId,
                    Nombre = r.AnimalName,
                    AnimalImage = r.AnimalImage,
                    Time = ParseTimeSafe(r.hora)
                })
                .OrderBy(r => r.fecha)
                .ThenBy(r => r.Time)
                .ThenBy(r => r.id)
                .ToList();

                // Build map: animalId -> list of posterior animalIds in chronological order
                var map = new Dictionary<int, List<int>>();
                var mapCount = new Dictionary<int, int>();
                var mapMaxFecha = new Dictionary<int, string>();
                var mapName = new Dictionary<int, string>();
                var mapImage = new Dictionary<int, string>();

                for (int i = 0; i < all.Count; i++)
                {
                    var baseRow = all[i];
                    int baseVal = baseRow.AnimalId;
                    if (!map.ContainsKey(baseVal))
                    {
                        map[baseVal] = new List<int>();
                        mapCount[baseVal] = 0;
                        mapMaxFecha[baseVal] = null;
                        mapName[baseVal] = baseRow.Nombre;
                        mapImage[baseVal] = baseRow.AnimalImage;
                    }

                    for (int j = i + 1; j < all.Count; j++)
                    {
                        var posterior = all[j];
                        map[baseVal].Add(posterior.AnimalId);
                        mapCount[baseVal]++;
                        mapMaxFecha[baseVal] = posterior.fecha;
                        if (posterior.AnimalId != baseVal)
                        {
                            break;
                        }
                    }
                }

                var result = map.Select(kv => new Models.ViewModels.AnimalitoResumen
                {
                    AnimalId = kv.Key,
                    Nombre = mapName.ContainsKey(kv.Key) ? mapName[kv.Key] : null,
                    ImageB64 = mapImage.ContainsKey(kv.Key) ? mapImage[kv.Key] : null,
                    AnimalPosteriores = kv.Value.Select(id => new Models.ViewModels.AnimalPosterior
                    {
                        AnimalId = id,
                        Nombre = mapName.ContainsKey(id) ? mapName[id] : null,
                        ImageB64 = mapImage.ContainsKey(id) ? mapImage[id] : null
                    }).ToList(),
                    Cantidad = mapCount.ContainsKey(kv.Key) ? mapCount[kv.Key] : 0,
                    UltimaFecha = mapMaxFecha.ContainsKey(kv.Key) ? mapMaxFecha[kv.Key] : null
                })
                .OrderBy(x => x.AnimalId)
                .ToList();

                return result;
            }
            catch (Exception ex)
            {
                var mensaje = $"HA OCURRIDO UN ERROR INTERNO: {ex.Message}";
                return new List<Models.ViewModels.AnimalitoResumen>();
            }
        }

        public async Task<List<Models.ViewModels.SeguimientoHorarioCandidate>> SeguimientoHorarioAsync(int hour,int? year = null, int? month = null)
        {
            try
            {
                // treat 0 as 'all' (no filter)
                if (year.HasValue && year.Value == 0) year = null;
                if (month.HasValue && month.Value == 0) month = null;
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Filter by optional year/month
                var query = db.lottoActivoResultados.AsNoTracking().AsQueryable();
                if (year.HasValue && month.HasValue)
                {
                    string prefix = year.Value + "-" + month.Value.ToString("D2") + "-";
                    query = query.Where(r => r.fecha.StartsWith(prefix));
                }

                // materialize filtered results and compute metrics in memory to be tolerant with hora formats
                var all = await query.ToListAsync();

                // frequency at the requested hour (parse hora safely)
                var freqAtHour = all
                    .Where(r => ParseTimeSafe(r.hora).Hours == hour)
                    //.GroupBy(r => r.lottoActivoAnimalId)
                    //.Select(g => new { AnimalId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.hora)
                    .ToList();

                // previous hour
                var prevHour = (hour + 23) % 24;

                // build mapping of next-of-prev: for each date order by time and look for records where an entry at prevHour is followed by another
                var nextCounts = new Dictionary<int,int>();
                var byDate = all.GroupBy(r => r.fecha);
                foreach (var group in byDate)
                {
                    var ordered = group.OrderBy(r => ParseTimeSafe(r.hora)).ThenBy(r => r.id).ToList();
                    for (int i = 0; i < ordered.Count - 1; i++)
                    {
                        if (ParseTimeSafe(ordered[i].hora).Hours == prevHour)
                        {
                            var next = ordered[i+1];
                            nextCounts.TryGetValue(next.lottoActivoAnimalId, out int c);
                            nextCounts[next.lottoActivoAnimalId] = c + 1;
                        }
                    }
                }

                // merge scores
                var candidates = new Dictionary<int, double>();
                foreach (var f in freqAtHour)
                {
                    //candidates[f.AnimalId] = f.Count;
                }
                foreach (var t in nextCounts)
                {
                    candidates[t.Key] = candidates.GetValueOrDefault(t.Key, 0) + t.Value * 0.5; // weight transitions
                }

                var top = freqAtHour.Select(kv => new Models.ViewModels.SeguimientoHorarioCandidate
                {
                    AnimalId = kv.lottoActivoAnimalId,
                    Nombre = db.lottoActivoAnimals.Where(a => a.id == kv.lottoActivoAnimalId).Select(a => a.nombre).FirstOrDefault(),
                    ImageB64 = db.lottoActivoAnimals.Where(a => a.id == kv.lottoActivoAnimalId).Select(a => a.image).FirstOrDefault(),
                    Score = 1
                }).ToList();

                return top;
            }
            catch
            {
                return new List<Models.ViewModels.SeguimientoHorarioCandidate>();
            }
        }



    }
}
