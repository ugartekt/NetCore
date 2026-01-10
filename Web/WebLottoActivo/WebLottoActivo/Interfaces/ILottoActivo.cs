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
        public Task<List<DesplazamientoResumen>> ListCantidadDesplazamientoAsync();
        public Task<List<DesplazamientoDiario>> ListDesplazamientoDiarioAsync(string date);
        public Task<List<LottoActivoAnimal>> ListLottoAnimal();
        public Task<List<DesplazamientoResumen>> ListDesplazamientoSeguidoAsync();
        public Task<List<CantidadTotalAnimalitos>> TotalHistorialAnimalito();

    }
}
