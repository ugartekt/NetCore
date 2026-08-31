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

        // Orden fisico de la rueda (ruleta americana: 0, 00, 1-36), expresado como
        // IDs de LottoActivoAnimal (37 = Delfin = casilla "0", 38 = Ballena = casilla "00").
        // Verificado contra sorteos reales: "desplazamiento" = distancia circular minima
        // en este arreglo entre el animalito actual y el del sorteo anterior.
        private static readonly int[] WheelAnimalIds = new int[]
        {
            37,28,9,26,30,11,7,20,32,17,5,22,34,15,3,24,36,13,1,38,
            27,10,25,29,12,8,19,31,18,6,21,33,16,4,23,35,14,2
        };

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
                    if (!map.ContainsKey(baseVal)) { map[baseVal] = new List<int>(); mapCount[baseVal] = 0; }
                    // "all" está ordenado cronológicamente ascendente, así que la última vez que
                    // baseVal aparece como baseRow deja aquí la fecha real de su última aparición.
                    mapMaxFecha[baseVal] = baseRow.fecha;

                    // include subsequent records until and including first different desplazamiento encountered
                    for (int j = i + 1; j < all.Count; j++)
                    {
                        var posterior = all[j];
                        map[baseVal].Add(posterior.desplazamiento);
                        mapCount[baseVal]++;
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
                        mapName[baseVal] = baseRow.Nombre;
                        mapImage[baseVal] = baseRow.AnimalImage;
                    }
                    // "all" está ordenado cronológicamente ascendente, así que la última vez que
                    // baseVal aparece como baseRow deja aquí la fecha real de su última aparición.
                    mapMaxFecha[baseVal] = baseRow.fecha;

                    for (int j = i + 1; j < all.Count; j++)
                    {
                        var posterior = all[j];
                        map[baseVal].Add(posterior.AnimalId);
                        mapCount[baseVal]++;
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

        public async Task<Models.ViewModels.SeguimientoHorarioViewModel> SeguimientoHorarioAsync(int hour,int? year = null, int? month = null)
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

                // frequency at the requested hour (parse hora safely). "hora" es texto, así que se
                // ordena por fecha y por ParseTimeSafe (no por el texto, que ordenaría mal
                // p.ej. "11:00AM" antes que "07:00PM").
                var freqAtHour = all
                    .Where(r => ParseTimeSafe(r.hora).Hours == hour)
                    .OrderByDescending(x => x.fecha)
                    .ThenByDescending(x => ParseTimeSafe(x.hora))
                    .ToList();

                // Veces reales que cada animal/desplazamiento salio en esta hora (dentro del filtro).
                var vecesPorAnimal = freqAtHour.GroupBy(f => f.lottoActivoAnimalId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var animalesLookup = await db.lottoActivoAnimals.AsNoTracking().ToListAsync();
                var animalPorId = animalesLookup.Where(a => a.id.HasValue).ToDictionary(a => a.id.Value, a => a);

                // El dia (del mes) en que salio cada sorteo, para mostrar "13|17|30" en las tablas de repetidos.
                int DiaDelMes(string fecha) => DateTime.TryParseExact(fecha, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d.Day : 0;

                // Un candidato por cada sorteo real de esta hora (sin deduplicar), mas reciente primero.
                var candidatos = freqAtHour.Select(f =>
                {
                    var animal = animalPorId.GetValueOrDefault(f.lottoActivoAnimalId);
                    return new Models.ViewModels.SeguimientoHorarioCandidate
                    {
                        AnimalId = f.lottoActivoAnimalId,
                        Nombre = animal?.nombre,
                        ImageB64 = animal?.image,
                        Veces = vecesPorAnimal.GetValueOrDefault(f.lottoActivoAnimalId, 0),
                        Desplazamiento = f.desplazamiento,
                        Dia = DiaDelMes(f.fecha)
                    };
                }).ToList();

                // Animales que se repitieron en esta hora, con los dias en que salieron (mas reciente primero).
                var animalesRepetidos = freqAtHour.GroupBy(f => f.lottoActivoAnimalId)
                    .Where(g => g.Count() > 1)
                    .Select(g =>
                    {
                        var animal = animalPorId.GetValueOrDefault(g.Key);
                        var ordenados = g.OrderBy(x => x.fecha).ThenBy(x => ParseTimeSafe(x.hora)).ToList();
                        return new Models.ViewModels.AnimalRepetidoHorario
                        {
                            AnimalId = g.Key,
                            Nombre = animal?.nombre,
                            ImageB64 = animal?.image,
                            Dias = string.Join("|", ordenados.Select(x => DiaDelMes(x.fecha))),
                            Cantidad = ordenados.Count,
                            UltimaFecha = ordenados.Last().fecha
                        };
                    })
                    .OrderByDescending(a => a.UltimaFecha)
                    .ToList();

                // Desplazamientos que se repitieron en esta hora, con los dias en que salieron (mas reciente primero).
                var desplazamientosRepetidos = freqAtHour.GroupBy(f => f.desplazamiento)
                    .Where(g => g.Count() > 1)
                    .Select(g =>
                    {
                        var ordenados = g.OrderBy(x => x.fecha).ThenBy(x => ParseTimeSafe(x.hora)).ToList();
                        return new Models.ViewModels.DesplazamientoRepetidoHorario
                        {
                            Desplazamiento = g.Key,
                            Dias = string.Join("|", ordenados.Select(x => DiaDelMes(x.fecha))),
                            Cantidad = ordenados.Count,
                            UltimaFecha = ordenados.Last().fecha
                        };
                    })
                    .OrderByDescending(d => d.UltimaFecha)
                    .ToList();

                return new Models.ViewModels.SeguimientoHorarioViewModel
                {
                    Candidatos = candidatos,
                    AnimalesRepetidos = animalesRepetidos,
                    DesplazamientosRepetidos = desplazamientosRepetidos
                };
            }
            catch
            {
                return new Models.ViewModels.SeguimientoHorarioViewModel();
            }
        }

        public async Task<List<Models.ViewModels.Occurrence>> GetOccurrencesAsync(int animalId, int? year = null, int? month = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var query = db.lottoActivoResultados.AsNoTracking().Where(r => r.lottoActivoAnimalId == animalId).AsQueryable();
                if (year.HasValue && month.HasValue)
                {
                    var prefix = year.Value + "-" + month.Value.ToString("D2") + "-";
                    query = query.Where(r => r.fecha.StartsWith(prefix));
                }

                // "hora" se guarda como texto (ej. "07:00PM"), así que se materializa primero y se
                // ordena en memoria con ParseTimeSafe para respetar el orden cronológico real
                // (un ORDER BY de texto pondría "11:00AM" antes que "07:00PM").
                var raw = await query.Select(r => new
                                     {
                                         r.id,
                                         r.fecha,
                                         r.hora,
                                         r.desplazamiento,
                                         AnimalNombre = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.nombre : null,
                                         AnimalImageB64 = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.image : null
                                     })
                                     .ToListAsync();

                var rows = raw.OrderByDescending(r => r.fecha).ThenByDescending(r => ParseTimeSafe(r.hora))
                              .Select(r => new Models.ViewModels.Occurrence
                              {
                                  Id = r.id,
                                  Fecha = r.fecha,
                                  Hora = r.hora,
                                  Desplazamiento = r.desplazamiento,
                                  Dias = 0,
                                  AnimalNombre = r.AnimalNombre,
                                  AnimalImageB64 = r.AnimalImageB64
                              })
                              .ToList();

                // compute Dias on the descending-ordered list: for each row, Dias = curr.Date - nextOlder.Date
                // rows are ordered descending by fecha,hora (newest first)
                for (int i = 0; i < rows.Count; i++)
                {
                    if (i == rows.Count - 1)
                    {
                        // oldest record, no older to compare
                        rows[i].Dias = 0;
                        continue;
                    }
                    DateTime? curr = DateTime.TryParse(rows[i].Fecha, out var cd) ? cd : (DateTime?)null;
                    DateTime? nextOlder = DateTime.TryParse(rows[i + 1].Fecha, out var nd) ? nd : (DateTime?)null;
                    if (curr.HasValue && nextOlder.HasValue)
                    {
                        rows[i].Dias = (curr.Value.Date - nextOlder.Value.Date).Days;
                    }
                    else
                    {
                        rows[i].Dias = 0;
                    }
                }

                // Animalito Posterior: el animal (y desplazamiento) que salio cronologicamente
                // despues de cada ocurrencia de este animal.
                if (rows.Count > 0)
                {
                    var secuencia = await db.lottoActivoResultados.AsNoTracking()
                                            .OrderBy(r => r.id)
                                            .Select(r => new
                                            {
                                                r.id,
                                                r.desplazamiento,
                                                AnimalNombre = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.nombre : null,
                                                AnimalImageB64 = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.image : null
                                            })
                                            .ToListAsync();

                    var siguientePorId = new Dictionary<int, (int? Desplazamiento, string AnimalNombre, string AnimalImageB64)>();
                    for (int i = 0; i < secuencia.Count; i++)
                    {
                        var siguienteInfo = (i + 1 < secuencia.Count)
                            ? (secuencia[i + 1].desplazamiento, secuencia[i + 1].AnimalNombre, secuencia[i + 1].AnimalImageB64)
                            : ((int?)null, (string)null, (string)null);
                        siguientePorId[secuencia[i].id] = siguienteInfo;
                    }

                    foreach (var row in rows)
                    {
                        if (siguientePorId.TryGetValue(row.Id, out var siguiente))
                        {
                            row.DesplazamientoPosterior = siguiente.Desplazamiento;
                            row.AnimalPosteriorNombre = siguiente.AnimalNombre;
                            row.AnimalPosteriorImageB64 = siguiente.AnimalImageB64;
                        }
                    }
                }

                return rows;
            }
            catch
            {
                return new List<Models.ViewModels.Occurrence>();
            }
        }

        public async Task<List<Models.ViewModels.Occurrence>> GetOccurrencesByDesplazamientoAsync(int desplazamiento, int? year = null, int? month = null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var query = db.lottoActivoResultados.AsNoTracking().Where(r => r.desplazamiento == desplazamiento).AsQueryable();
                if (year.HasValue && month.HasValue)
                {
                    var prefix = year.Value + "-" + month.Value.ToString("D2") + "-";
                    query = query.Where(r => r.fecha.StartsWith(prefix));
                }

                // ver comentario equivalente en GetOccurrencesAsync: "hora" es texto y no se puede
                // ordenar cronológicamente en el propio SQL, se materializa y se ordena en memoria.
                var raw = await query.Select(r => new
                                     {
                                         r.id,
                                         r.fecha,
                                         r.hora,
                                         r.desplazamiento,
                                         AnimalNombre = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.nombre : null,
                                         AnimalImageB64 = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.image : null
                                     })
                                     .ToListAsync();

                var rows = raw.OrderByDescending(r => r.fecha).ThenByDescending(r => ParseTimeSafe(r.hora))
                              .Select(r => new Models.ViewModels.Occurrence
                              {
                                  Id = r.id,
                                  Fecha = r.fecha,
                                  Hora = r.hora,
                                  Desplazamiento = r.desplazamiento,
                                  Dias = 0,
                                  AnimalNombre = r.AnimalNombre,
                                  AnimalImageB64 = r.AnimalImageB64
                              })
                              .ToList();

                // compute Dias on descending list
                for (int i = 0; i < rows.Count; i++)
                {
                    if (i == rows.Count - 1)
                    {
                        rows[i].Dias = 0;
                        continue;
                    }
                    DateTime? curr = DateTime.TryParse(rows[i].Fecha, out var cd) ? cd : (DateTime?)null;
                    DateTime? nextOlder = DateTime.TryParse(rows[i + 1].Fecha, out var nd) ? nd : (DateTime?)null;
                    if (curr.HasValue && nextOlder.HasValue)
                    {
                        rows[i].Dias = (curr.Value.Date - nextOlder.Value.Date).Days;
                    }
                    else
                    {
                        rows[i].Dias = 0;
                    }
                }

                // Desplazamiento Posterior: el desplazamiento del resultado que salio
                // cronologicamente despues de cada ocurrencia (sin filtrar por desplazamiento).
                if (rows.Count > 0)
                {
                    var secuencia = await db.lottoActivoResultados.AsNoTracking()
                                            .OrderBy(r => r.id)
                                            .Select(r => new
                                            {
                                                r.id,
                                                r.desplazamiento,
                                                AnimalNombre = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.nombre : null,
                                                AnimalImageB64 = r.LottoActivoAnimal != null ? r.LottoActivoAnimal.image : null
                                            })
                                            .ToListAsync();

                    var siguientePorId = new Dictionary<int, (int? Desplazamiento, string AnimalNombre, string AnimalImageB64)>();
                    for (int i = 0; i < secuencia.Count; i++)
                    {
                        var siguienteInfo = (i + 1 < secuencia.Count)
                            ? (secuencia[i + 1].desplazamiento, secuencia[i + 1].AnimalNombre, secuencia[i + 1].AnimalImageB64)
                            : ((int?)null, (string)null, (string)null);
                        siguientePorId[secuencia[i].id] = siguienteInfo;
                    }

                    foreach (var row in rows)
                    {
                        if (siguientePorId.TryGetValue(row.Id, out var siguiente))
                        {
                            row.DesplazamientoPosterior = siguiente.Desplazamiento;
                            row.AnimalPosteriorNombre = siguiente.AnimalNombre;
                            row.AnimalPosteriorImageB64 = siguiente.AnimalImageB64;
                        }
                    }
                }

                return rows;
            }
            catch
            {
                return new List<Models.ViewModels.Occurrence>();
            }
        }

        public async Task<ProximaRondaViewModel> GetProximaRondaAsync()
        {
            try
            {
                var ultimo = await UltimoAnimalitoDesplazamientoAsync();
                if (ultimo == null) return new ProximaRondaViewModel();

                int total = WheelAnimalIds.Length;
                int posActual = Array.IndexOf(WheelAnimalIds, ultimo.lottoActivoAnimalId);
                if (posActual < 0) return new ProximaRondaViewModel();

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var animales = await db.lottoActivoAnimals.AsNoTracking().ToListAsync();
                var animalPorId = animales.Where(a => a.id.HasValue).ToDictionary(a => a.id.Value, a => a);

                var items = new List<ProximaRondaItem>();
                for (int offset = 1; offset <= total / 2; offset++)
                {
                    int idxDer = (posActual + offset) % total;
                    int idxIzq = ((posActual - offset) % total + total) % total;

                    var animalDer = animalPorId.GetValueOrDefault(WheelAnimalIds[idxDer]);
                    var animalIzq = animalPorId.GetValueOrDefault(WheelAnimalIds[idxIzq]);

                    items.Add(new ProximaRondaItem
                    {
                        Offset = offset,
                        AnimalIzqId = WheelAnimalIds[idxIzq],
                        AnimalIzqNombre = animalIzq?.nombre,
                        AnimalIzqImageB64 = animalIzq?.image,
                        AnimalDerId = WheelAnimalIds[idxDer],
                        AnimalDerNombre = animalDer?.nombre,
                        AnimalDerImageB64 = animalDer?.image
                    });
                }

                var animalActual = animalPorId.GetValueOrDefault(ultimo.lottoActivoAnimalId);

                return new ProximaRondaViewModel
                {
                    UltimoAnimalId = ultimo.lottoActivoAnimalId,
                    UltimoAnimalNombre = animalActual?.nombre,
                    UltimoAnimalImageB64 = animalActual?.image,
                    UltimoDesplazamiento = ultimo.desplazamiento,
                    UltimaFecha = ultimo.fecha,
                    UltimaHora = ultimo.hora,
                    Items = items
                };
            }
            catch
            {
                return new ProximaRondaViewModel();
            }
        }

        public async Task<PrediccionViewModel> GetPrediccionAsync(int dias = 3)
        {
            try
            {
                if (dias <= 0) dias = 3;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var all = await db.lottoActivoResultados.AsNoTracking()
                                  .Select(r => new { r.id, r.fecha, r.hora, r.desplazamiento, r.lottoActivoAnimalId })
                                  .ToListAsync();

                var hoy = DateTime.Today;
                string cutoff = hoy.AddDays(-(dias - 1)).ToString("yyyy-MM-dd");

                var desplazamientos = new List<PrediccionItem>();
                for (int d = 0; d <= 19; d++)
                {
                    var filas = all.Where(r => r.desplazamiento == d)
                                   .OrderByDescending(r => r.fecha).ThenByDescending(r => ParseTimeSafe(r.hora))
                                   .ToList();
                    var ultimo = filas.FirstOrDefault();
                    int diasSinSalir = (ultimo != null && DateTime.TryParse(ultimo.fecha, out var uf))
                        ? (hoy - uf.Date).Days
                        : int.MaxValue;
                    int frecuencia = filas.Count(r => r.fecha.CompareTo(cutoff) >= 0);

                    desplazamientos.Add(new PrediccionItem
                    {
                        DesplazamientoValor = d,
                        Nombre = d.ToString("D2"),
                        UltimaFecha = ultimo?.fecha,
                        UltimaHora = ultimo?.hora,
                        DiasSinSalir = diasSinSalir,
                        FrecuenciaUltimosDias = frecuencia,
                        EsCandidato = diasSinSalir >= dias
                    });
                }

                var animales = await db.lottoActivoAnimals.AsNoTracking().OrderBy(a => a.id).ToListAsync();
                var animalitos = new List<PrediccionItem>();
                foreach (var animal in animales)
                {
                    if (!animal.id.HasValue) continue;
                    var filas = all.Where(r => r.lottoActivoAnimalId == animal.id.Value)
                                   .OrderByDescending(r => r.fecha).ThenByDescending(r => ParseTimeSafe(r.hora))
                                   .ToList();
                    var ultimo = filas.FirstOrDefault();
                    int diasSinSalir = (ultimo != null && DateTime.TryParse(ultimo.fecha, out var uf))
                        ? (hoy - uf.Date).Days
                        : int.MaxValue;
                    int frecuencia = filas.Count(r => r.fecha.CompareTo(cutoff) >= 0);

                    animalitos.Add(new PrediccionItem
                    {
                        AnimalId = animal.id,
                        Nombre = animal.nombre,
                        ImageB64 = animal.image,
                        UltimaFecha = ultimo?.fecha,
                        UltimaHora = ultimo?.hora,
                        DiasSinSalir = diasSinSalir,
                        FrecuenciaUltimosDias = frecuencia,
                        EsCandidato = diasSinSalir >= dias
                    });
                }

                return new PrediccionViewModel
                {
                    Dias = dias,
                    Desplazamientos = desplazamientos.OrderByDescending(x => x.DiasSinSalir).ToList(),
                    Animalitos = animalitos.OrderByDescending(x => x.DiasSinSalir).ToList()
                };
            }
            catch
            {
                return new PrediccionViewModel();
            }
        }

    }
}
