namespace WebLottoActivo.Models.ViewModels
{
    public class DesplazamientoDiario
    {
        public string Nombre { get; set; }
        public string ImageB64 { get; set; }
        public int Desplazamiento { get; set; }
        public string Hora { get; set; }
        public bool IsFlag { get; set; }
    }
}
