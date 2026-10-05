using Microsoft.AspNetCore.Mvc;
using WebLottoActivo.Interfaces;
using WebLottoActivo.Models;
using WebLottoActivo.Models.ViewModels;
using WebLottoActivo.Service;

namespace WebLottoActivo.Controllers
{
    public class LottoActivoController : Controller
    {
        private ILottoActivo _lottoActivo;

        public string MSJ = string.Empty;
        public LottoActivoController(ILottoActivo lottoActivo)
        {
            _lottoActivo = lottoActivo;
        }
        [HttpGet]
        public async Task<IActionResult> Occurrences(int animalId, int? year, int? month)
        {
            // default to selected month/year if not provided; 0 significa "Todas" (sin filtro)
            var today = DateTime.Today;
            int? useYear = year.HasValue ? (year.Value == 0 ? (int?)null : year.Value) : today.Year;
            int? useMonth = month.HasValue ? (month.Value == 0 ? (int?)null : month.Value) : today.Month;

            var rows = await _lottoActivo.GetOccurrencesAsync(animalId, useYear, useMonth);
            return Json(rows);
        }

        [HttpGet]
        public async Task<IActionResult> OccurrencesByDesplazamiento(int desplazamiento, int? year, int? month)
        {
            // 0 significa "Todas" (sin filtro)
            var today = DateTime.Today;
            int? useYear = year.HasValue ? (year.Value == 0 ? (int?)null : year.Value) : today.Year;
            int? useMonth = month.HasValue ? (month.Value == 0 ? (int?)null : month.Value) : today.Month;

            var rows = await _lottoActivo.GetOccurrencesByDesplazamientoAsync(desplazamiento, useYear, useMonth);
            return Json(rows);
        }
        public async Task<IActionResult> Index()
        {
            List<LottoActivoAnimal> listLottoActivoAnimal = await _lottoActivo.ListLottoAnimal();
            return View(listLottoActivoAnimal);
        }

        public async Task<IActionResult> DesplazamientoResumen(int rango = 0)
        {
            // default to current month
            var today = DateTime.Today;
            int? year = today.Year;
            int? month = today.Month;

            // For display purposes, keep the raw selection (0 = "Todas") separate from the
            // nulled-out filter values below, para que el combo no vuelva a mostrar el año/mes
            // actual cuando el usuario elige "Todas".
            int displayYear = today.Year;
            int displayMonth = today.Month;

            // allow query params year/month; treat 0 as 'Todas' (null)
            if (Request.Query.ContainsKey("year"))
            {
                if (int.TryParse(Request.Query["year"], out var y)) { displayYear = y; year = y == 0 ? null : (int?)y; }
            }
            if (Request.Query.ContainsKey("month"))
            {
                if (int.TryParse(Request.Query["month"], out var m)) { displayMonth = m; month = m == 0 ? null : (int?)m; }
            }

            // For display purposes, pick values or defaults
            ViewBag.SelectedYear = displayYear;
            ViewBag.SelectedMonth = displayMonth;

            // provide available year range to the view
            var range = await _lottoActivo.GetAvailableYearRangeAsync();
            ViewBag.MinYear = range.minYear;
            ViewBag.MaxYear = range.maxYear;

            List<DesplazamientoResumen> listDesplazamientoResumens = await _lottoActivo.ListCantidadDesplazamientoAsync(year, month);
            return View(listDesplazamientoResumens);
        }
        public async Task<IActionResult> DesplazamientoSeguido()
        {
            List<DesplazamientoResumen> listDesplazamientoResumens = await _lottoActivo.ListDesplazamientoSeguidoAsync();
            return View(listDesplazamientoResumens);
        }

        public async Task<IActionResult> AnimalitoResumen(int? year, int? month)
        {
            // default to current month
            var today = DateTime.Today;
            int selectedYear = year ?? today.Year;
            int selectedMonth = month ?? today.Month;

            var result = await _lottoActivo.ListCantidadAnimalitoAsync(selectedYear, selectedMonth);

            // also get total occurrences per animal for the selected month to show in the UI
            var totals = await _lottoActivo.TotalHistorialAnimalito(selectedYear, selectedMonth);
            var countByAnimal = totals.ToDictionary(t => t.Id, t => t.Cantidad);
            ViewBag.CountByAnimal = countByAnimal;

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;
            return View(result);
        }

        public async Task<IActionResult> ResumenDiario(string date)
        {

            // Normalize dates: if no date provided use today, otherwise use provided date
            if (string.IsNullOrEmpty(date))
            {
                date = DateTime.Today.ToString("yyyy-MM-dd");
                ViewBag.Today = date;
                ViewBag.Yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.Today = date;
                ViewBag.Yesterday = Convert.ToDateTime(date).AddDays(-1).ToString("yyyy-MM-dd");
            }

            string dateYesterday = ViewBag.Yesterday;

            var today = await _lottoActivo.ListDesplazamientoDiarioAsync(date);
            var yesterday = await _lottoActivo.ListDesplazamientoDiarioAsync(dateYesterday);

            // Consider repeats by LottoActivoAnimalId only for the "Animales repetidos" card.
            var comunesByAnimal = today.Select(x => x.LottoActivoAnimalId)
                                       .Intersect(yesterday.Select(y => y.LottoActivoAnimalId))
                                       .ToHashSet();

            var comunes = today.Select(x => x.Desplazamiento)
                                           .Intersect(yesterday.Select(y => y.Desplazamiento))
                                           .ToHashSet();

            // mark flags in the tables when animal id repeats
            foreach (var item in today)
                item.IsFlag = comunes.Contains(item.Desplazamiento);

            foreach (var item in yesterday)
                item.IsFlag = comunes.Contains(item.Desplazamiento);



            // Helper to parse hora in several formats (HH:mm, hh:mmAM/PM, etc.)
            TimeSpan ParseHora(string hora)
            {
                if (string.IsNullOrEmpty(hora)) return TimeSpan.Zero;
                if (DateTime.TryParse(hora, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var dt)) return dt.TimeOfDay;
                if (TimeSpan.TryParse(hora, out var ts)) return ts;
                var parts = hora.Split(':');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int ph) && int.TryParse(parts[1], out int pm)) return new TimeSpan(ph, pm, 0);
                return TimeSpan.Zero;
            }

            // Build list of repeated animals ordered by date (yesterday first) then by hora
            var combined = yesterday.Select(x => new { Item = x, Day = 0, Time = ParseHora(x.Hora) })
                                    .Concat(today.Select(x => new { Item = x, Day = 1, Time = ParseHora(x.Hora) }))
                                    .Where(x => comunesByAnimal.Contains(x.Item.LottoActivoAnimalId))
                                    .OrderByDescending(x => x.Day)
                                    .ThenBy(x => x.Time)
                                    .ThenBy(x => x.Item.LottoActivoAnimalId)
                                    .ToList();

            var repeated = combined.GroupBy(x => x.Item.LottoActivoAnimalId)
                                   .Select(g => g.First().Item)
                                   .ToList();

            var repeatedCounts = repeated.ToDictionary(r => r.LottoActivoAnimalId,
                r => today.Count(t => t.LottoActivoAnimalId == r.LottoActivoAnimalId) + yesterday.Count(t => t.LottoActivoAnimalId == r.LottoActivoAnimalId));

            var viewModel = new ResumenDiario
            {
                Today = today,
                Yesterday = yesterday,
                Repeated = repeated,
                RepeatedCounts = repeatedCounts
            };

            return View(viewModel);
        }


        //View to display seguimiento horario candidates and controls
        [HttpGet]
        public async Task<IActionResult> SeguimientoHorario(int? hour, int? year, int? month)
        {
            var today = DateTime.Today;
            int selHour = hour ?? 8;

            // prepare year/month defaults
            year = year ?? today.Year;
            month = month ?? today.Month;

            var range = await _lottoActivo.GetAvailableYearRangeAsync();
            ViewBag.MinYear = range.minYear;
            ViewBag.MaxYear = range.maxYear;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedHour = selHour;

            var model = await _lottoActivo.SeguimientoHorarioAsync(selHour, year, month);
            return View("SeguimientoHorario", model);
        }

        public async Task<IActionResult> HistorialResumenDiario(string date)
        {
            date = (date == null) ? DateTime.Now.ToString("yyyy-MM-dd") : date;

            ViewBag.FechaSeleccionada = date;

            List<DesplazamientoDiario> listDesplazamientoDiario = await _lottoActivo.ListDesplazamientoDiarioAsync(date);

            return View(listDesplazamientoDiario);
        }
        public async Task<IActionResult> TotalHistorialAnimalito(int? year, int? month)
        {
            // determine selected year/month (defaults to current)
            var today = DateTime.Today;
            // treat 0 as 'Todas' (null)
            int? selYear = (year.HasValue && year.Value == 0) ? null : year;
            int? selMonth = (month.HasValue && month.Value == 0) ? null : month;

            int displayYear = year ?? today.Year;
            int displayMonth = month ?? today.Month;

            List<CantidadTotalAnimalitos> cantidadTotalAnimalitos = await _lottoActivo.TotalHistorialAnimalito(selYear, selMonth);
            ViewBag.SelectedYear = displayYear;
            ViewBag.SelectedMonth = displayMonth;

            var range = await _lottoActivo.GetAvailableYearRangeAsync();
            ViewBag.MinYear = range.minYear;
            ViewBag.MaxYear = range.maxYear;
            return View(cantidadTotalAnimalitos);
        }

        public async Task<IActionResult> ProximaRondaModal()
        {
            var model = await _lottoActivo.GetProximaRondaAsync();
            return PartialView("_ProximaRondaModal", model);
        }

        public async Task<IActionResult> Repeticion(string date, int desfase = 2, int ventana = 30)
        {
            var model = await _lottoActivo.GetRepeticionAsync(date, desfase, ventana);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> HistorialPatron(int id, string date, int desfase = 2)
        {
            var rows = await _lottoActivo.GetHistorialPatronAsync(id, date, desfase);
            return Json(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPatron(string codigo, List<string> horas, string date, int desfase = 2, int ventana = 30)
        {
            var error = await _lottoActivo.CrearPatronAsync(codigo, horas);
            if (error != null) TempData["PatronError"] = error;
            return RedirectToAction(nameof(Repeticion), new { date, desfase, ventana });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPatron(int id, string date, int desfase = 2, int ventana = 30)
        {
            await _lottoActivo.EliminarPatronAsync(id);
            return RedirectToAction(nameof(Repeticion), new { date, desfase, ventana });
        }

        public async Task<IActionResult> Prediccion(int dias = 3)
        {
            if (dias <= 0) dias = 3;
            var model = await _lottoActivo.GetPrediccionAsync(dias);
            return View(model);
        }

    }
}
