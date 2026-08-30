using System.Collections.Generic;

namespace WebLottoActivo.Models.ViewModels
{
    public class ProximaRondaViewModel
    {
        public int UltimoAnimalId { get; set; }
        public string UltimoAnimalNombre { get; set; }
        public string UltimoAnimalImageB64 { get; set; }
        public int UltimoDesplazamiento { get; set; }
        public string UltimaFecha { get; set; }
        public string UltimaHora { get; set; }
        public List<ProximaRondaItem> Items { get; set; } = new List<ProximaRondaItem>();
    }
}
