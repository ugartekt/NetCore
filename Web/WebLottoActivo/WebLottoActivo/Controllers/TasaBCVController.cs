using Microsoft.AspNetCore.Mvc;
using WebLottoActivo.Interfaces;
using WebLottoActivo.Models;

namespace WebLottoActivo.Controllers
{
    public class TasaBCVController : Controller
    {
        private IBCVTasa _bcvtasa;

        public string MSJ = string.Empty;
        public TasaBCVController(IBCVTasa bcvtasa)
        {
            _bcvtasa = bcvtasa;
        }
        public async Task<IActionResult> Index()
        {
            List<TasaBCV> listTasaBCV = await _bcvtasa.ListTasaCurrentAsync();
            return View(listTasaBCV);
        }
        public async Task<IActionResult> IndexUSD()
        {
            List<TasaBCV> listTasaBCV = await _bcvtasa.ListTasaUSDAsync();
            return View(listTasaBCV);
        }
        public async Task<IActionResult> IndexEUR()
        {
            List<TasaBCV> listTasaBCV = await _bcvtasa.ListTasaEURAsync();
            return View(listTasaBCV);
        }
    }
}
