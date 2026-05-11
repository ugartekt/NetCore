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

            // allow query params year/month; treat 0 as 'Todas' (null)
            if (Request.Query.ContainsKey("year"))
            {
                if (int.TryParse(Request.Query["year"], out var y)) year = y == 0 ? null : (int?)y;
            }
            if (Request.Query.ContainsKey("month"))
            {
                if (int.TryParse(Request.Query["month"], out var m)) month = m == 0 ? null : (int?)m;
            }

            // For display purposes, pick values or defaults
            ViewBag.SelectedYear = year ?? today.Year;
            ViewBag.SelectedMonth = month ?? today.Month;

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

            // mark flags in the tables when animal id repeats
            foreach (var item in today)
                item.IsFlag = comunesByAnimal.Contains(item.LottoActivoAnimalId);

            foreach (var item in yesterday)
                item.IsFlag = comunesByAnimal.Contains(item.LottoActivoAnimalId);

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

            // Build list of repeated animals ordered by earliest hora across both days
            var combined = today.Concat(yesterday)
                                .Where(x => comunesByAnimal.Contains(x.LottoActivoAnimalId))
                                .Select(x => new { Item = x, Time = ParseHora(x.Hora) })
                                .OrderBy(x => x.Time)
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
        public async Task<IActionResult> SeguimientoHorario(int? hour, int? topN, int? year, int? month)
        {
            var today = DateTime.Today;
            int selHour = hour ?? 19;
            int selTop = topN ?? 5;

            // prepare year/month defaults
            int displayYear = year ?? today.Year;
            int displayMonth = month ?? today.Month;

            var range = await _lottoActivo.GetAvailableYearRangeAsync();
            ViewBag.MinYear = range.minYear;
            ViewBag.MaxYear = range.maxYear;
            ViewBag.SelectedYear = displayYear;
            ViewBag.SelectedMonth = displayMonth;
            ViewBag.SelectedHour = selHour;
            ViewBag.SelectedTop = selTop;

            var preds = await _lottoActivo.SeguimientoHorarioAsync(selHour, selTop, year, month);
            return View("SeguimientoHorario", preds);
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

    }
}
