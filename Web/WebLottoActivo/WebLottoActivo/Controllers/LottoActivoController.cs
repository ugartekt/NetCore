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
            List<DesplazamientoResumen> listDesplazamientoResumens = await _lottoActivo.ListCantidadDesplazamientoAsync();
            return View(listDesplazamientoResumens);
        }
        public async Task<IActionResult> DesplazamientoSeguido()
        {
            List<DesplazamientoResumen> listDesplazamientoResumens = await _lottoActivo.ListDesplazamientoSeguidoAsync();
            return View(listDesplazamientoResumens);
        }

        public async Task<IActionResult> ResumenDiario(string date)
        {

            date = string.IsNullOrEmpty(date) ? DateTime.Today.ToString("yyyy-MM-dd"): date;
            string dateYesterday = string.IsNullOrEmpty(date) ? DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") : Convert.ToDateTime(date).AddDays(-1).ToString("yyyy-MM-dd");

            var today = await _lottoActivo.ListDesplazamientoDiarioAsync(date);
            var yesterday = await _lottoActivo.ListDesplazamientoDiarioAsync(DateTime.Today.AddDays(-1).ToString(dateYesterday));

            var comunes = today.Select(x => x.Desplazamiento)
                               .Intersect(yesterday.Select(y => y.Desplazamiento))
                               .ToHashSet();

            foreach (var item in today)
                item.IsFlag = comunes.Contains(item.Desplazamiento);

            foreach (var item in yesterday)
                item.IsFlag = comunes.Contains(item.Desplazamiento);

            ViewBag.Today = date;
            ViewBag.Yesterday = dateYesterday;

            var viewModel = new ResumenDiario
            {
                Today = today,
                Yesterday = yesterday
            };

            return View(viewModel);
        }

        public async Task<IActionResult> HistorialResumenDiario(string date)
        {
            date = (date == null) ? DateTime.Now.ToString("yyyy-MM-dd") : date;

            ViewBag.FechaSeleccionada = date;

            List<DesplazamientoDiario> listDesplazamientoDiario = await _lottoActivo.ListDesplazamientoDiarioAsync(date);

            return View(listDesplazamientoDiario);
        }
        public async Task<IActionResult> TotalHistorialAnimalito()
        {
            List<CantidadTotalAnimalitos> cantidadTotalAnimalitos = await _lottoActivo.TotalHistorialAnimalito();
            return View(cantidadTotalAnimalitos);
        }

    }
}
