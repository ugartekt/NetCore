using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerServicesLocal.Models;

namespace WorkerServicesLocal.Interfaces
{
    public interface ILottoActivo
    {
        public Task<bool> InsertResultadoAsync(LottoActivoResultado lottoActivoResultado);
        public Task<LottoActivoResultado> UltimoAnimalitoDesplazamientoAsync();

        public Task<LottoActivoResultado> SearchResultadosAsync(string date, string hora);
    }
}
