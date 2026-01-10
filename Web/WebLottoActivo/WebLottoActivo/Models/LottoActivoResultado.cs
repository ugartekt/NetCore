namespace WebLottoActivo.Models
{
    public class LottoActivoResultado
    {
        public int id { get; set; }
        public int lottoActivoAnimalId { get; set; }
        public int desplazamiento { get; set; }
        public string fecha { get; set; }
        public string hora { get; set; }
        public LottoActivoAnimal LottoActivoAnimal { get; set; }
    }
}
