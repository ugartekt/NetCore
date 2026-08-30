namespace WebLottoActivo.Models.ViewModels
{
    public class PrediccionItem
    {
        public int? DesplazamientoValor { get; set; }
        public int? AnimalId { get; set; }
        public string Nombre { get; set; }
        public string ImageB64 { get; set; }
        public string UltimaFecha { get; set; }
        public string UltimaHora { get; set; }
        public int DiasSinSalir { get; set; }
        public int FrecuenciaUltimosDias { get; set; }
        public bool EsCandidato { get; set; }
    }
}
