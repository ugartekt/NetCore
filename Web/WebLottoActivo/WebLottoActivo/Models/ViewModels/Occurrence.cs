namespace WebLottoActivo.Models.ViewModels
{
    public class Occurrence
    {
        public int Id { get; set; }
        public string Fecha { get; set; }
        public string Hora { get; set; }
        public int Desplazamiento { get; set; }
        public int? DesplazamientoPosterior { get; set; }
        public int Dias { get; set; }
        public string AnimalNombre { get; set; }
        public string AnimalImageB64 { get; set; }
        public string AnimalPosteriorNombre { get; set; }
        public string AnimalPosteriorImageB64 { get; set; }
    }
}
