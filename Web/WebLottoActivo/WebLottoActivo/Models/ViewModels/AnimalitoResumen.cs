namespace WebLottoActivo.Models.ViewModels
{
    public class AnimalitoResumen
    {
        public int AnimalId { get; set; }
        public string Nombre { get; set; }
        public string ImageB64 { get; set; }
        public List<AnimalPosterior> AnimalPosteriores { get; set; }
        public int Cantidad { get; set; }
        public string UltimaFecha { get; set; }
    }
}
