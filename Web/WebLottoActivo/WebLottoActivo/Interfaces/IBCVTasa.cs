using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLottoActivo.Models;

namespace WebLottoActivo.Interfaces
{
    public interface IBCVTasa
    {
        public Task<bool> InsertTasaAsync(TasaBCV tasaBCV);
        public Task<TasaBCV> SearchTasaAsync(string date);
        public Task<List<TasaBCV>> ListTasaUSDAsync();
        public Task<List<TasaBCV>> ListTasaEURAsync();
        public Task<List<TasaBCV>> ListTasaCurrentAsync();
    }
}
