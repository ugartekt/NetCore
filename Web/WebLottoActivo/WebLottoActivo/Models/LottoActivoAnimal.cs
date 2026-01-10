namespace WebLottoActivo.Models
{
    public class LottoActivoAnimal
    {
        public int? id { get; set; }
        public string? nombre { get; set; }
        public string? image { get; set; }
        public ICollection<LottoActivoResultado> Resultados { get; set; }

    }

}
