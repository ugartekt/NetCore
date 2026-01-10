using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerServicesLocal.Models;

namespace WorkerServicesLocal.Interfaces
{
    public interface IBCVTasa
    {
        public Task<bool> InsertTasaAsync(TasaBCV tasaBCV);
        public Task<TasaBCV> SearchTasaAsync(string date);
    }
}
