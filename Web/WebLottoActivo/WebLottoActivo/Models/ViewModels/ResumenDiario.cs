namespace WebLottoActivo.Models.ViewModels
{
    public class ResumenDiario
    {
        public List<DesplazamientoDiario> Today { get; set; }
        public List<DesplazamientoDiario> Yesterday { get; set; }
        public List<DesplazamientoDiario> Repeated { get; set; }
        public Dictionary<int,int> RepeatedCounts { get; set; }

        public string DateToday { get; set; }
        public string DateYesterday { get; set; }

    }
}
