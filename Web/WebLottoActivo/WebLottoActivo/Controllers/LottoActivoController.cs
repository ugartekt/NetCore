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

            // Compare by LottoActivoAnimalId instead of desplazamiento to detect repeated animals
            var comunes = today.Select(x => x.LottoActivoAnimalId)
                               .Intersect(yesterday.Select(y => y.LottoActivoAnimalId))
                               .ToHashSet();

            foreach (var item in today)
                item.IsFlag = comunes.Contains(item.LottoActivoAnimalId);

            foreach (var item in yesterday)
                item.IsFlag = comunes.Contains(item.LottoActivoAnimalId);

            // Build a deterministic list of repeated animals (prefer a record from 'today' when available)
            var repeated = new List<DesplazamientoDiario>();
            var repeatedCounts = new Dictionary<int,int>();
            // Preserve first-seen order: take ids in the order they appear in 'today' then 'yesterday'
            var repeatedIds = today.Concat(yesterday)
                                    .Select(x => x.LottoActivoAnimalId)
                                    .Where(id => comunes.Contains(id))
                                    .Distinct()
                                    .ToList();
            foreach (var id in repeatedIds)
            {
                var item = today.FirstOrDefault(t => t.LottoActivoAnimalId == id) ?? yesterday.FirstOrDefault(t => t.LottoActivoAnimalId == id);
                if (item != null)
                    repeated.Add(item);
                // count occurrences across both lists
                int count = today.Count(t => t.LottoActivoAnimalId == id) + yesterday.Count(t => t.LottoActivoAnimalId == id);
                repeatedCounts[id] = count;
            }

            var viewModel = new ResumenDiario
            {
                Today = today,
                Yesterday = yesterday,
                Repeated = repeated,
                RepeatedCounts = repeatedCounts
            };

            return View(viewModel);
        }

        //[HttpGet]
        //public async Task<IActionResult> SeguimientoHorario(int hour = 19, int topN = 5, int? year = null, int? month = null)
        //{
        //    var preds = await _lottoActivo.SeguimientoHorarioAsync(hour, topN, year, month);
        //    return Json(preds);
        //}

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
