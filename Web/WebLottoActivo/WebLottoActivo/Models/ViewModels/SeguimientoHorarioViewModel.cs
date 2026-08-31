using System.Collections.Generic;

namespace WebLottoActivo.Models.ViewModels
{
    public class SeguimientoHorarioViewModel
    {
        public List<SeguimientoHorarioCandidate> Candidatos { get; set; } = new List<SeguimientoHorarioCandidate>();
        public List<AnimalRepetidoHorario> AnimalesRepetidos { get; set; } = new List<AnimalRepetidoHorario>();
        public List<DesplazamientoRepetidoHorario> DesplazamientosRepetidos { get; set; } = new List<DesplazamientoRepetidoHorario>();
    }
}
