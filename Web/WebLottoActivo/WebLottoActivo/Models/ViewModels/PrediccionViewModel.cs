using System.Collections.Generic;

namespace WebLottoActivo.Models.ViewModels
{
    public class PrediccionViewModel
    {
        public int Dias { get; set; }
        public List<PrediccionItem> Desplazamientos { get; set; } = new List<PrediccionItem>();
        public List<PrediccionItem> Animalitos { get; set; } = new List<PrediccionItem>();
    }
}
