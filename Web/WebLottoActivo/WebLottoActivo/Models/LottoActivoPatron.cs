namespace WebLottoActivo.Models
{
    public class LottoActivoPatron
    {
        public int id { get; set; }
        public string codigo { get; set; }
        // Horas separadas por coma, en el mismo formato que LottoActivoResultado.hora (ej: "08:00AM,12:00PM,03:00PM")
        public string horas { get; set; }
    }
}
