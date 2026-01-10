using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerServicesLocal.Models
{
    public class LottoActivoResultado
    {
        public int? id { get; set; }
        public int? lottoActivoAnimalId { get; set; }
        public int? desplazamiento { get; set; }
        public string? fecha { get; set; }
        public string? hora { get; set; }
    }

}
