using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLottoActivo.Models;
using WebLottoActivo.Models.ViewModels;

namespace WebLottoActivo.Interfaces
{
    public interface ILottoActivo
    {
        public Task<bool> InsertResultadoAsync(LottoActivoResultado lottoActivoResultado);
        public Task<LottoActivoResultado> UltimoAnimalitoDesplazamientoAsync();
        public Task<List<DesplazamientoResumen>> ListCantidadDesplazamientoAsync(int? year = null, int? month = null);
        public Task<List<Models.ViewModels.AnimalitoResumen>> ListCantidadAnimalitoAsync(int? year = null, int? month = null);
        public Task<List<DesplazamientoDiario>> ListDesplazamientoDiarioAsync(string date);
        public Task<List<LottoActivoAnimal>> ListLottoAnimal();
        public Task<List<DesplazamientoResumen>> ListDesplazamientoSeguidoAsync();
        public Task<List<CantidadTotalAnimalitos>> TotalHistorialAnimalito(int? year = null, int? month = null);
        public Task<(int minYear, int maxYear)> GetAvailableYearRangeAsync();
        public Task<List<Models.ViewModels.SeguimientoHorarioCandidate>> SeguimientoHorarioAsync(int hour, int? year = null, int? month = null);

    }
}
