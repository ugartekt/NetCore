using System.Collections.Generic;
using System.Linq;

namespace WebLottoActivo.Models.ViewModels
{
    public class RepeticionViewModel
    {
        public string Fecha { get; set; }
        public string FechaOrigen { get; set; }
        public int Desfase { get; set; }
        // Días hacia atrás que se usan para las estadísticas (7, 15, 30, 45 o 60)
        public int Ventana { get; set; }

        // % histórico de animalitos del día origen que vuelven a salir "Desfase" días después
        public double TasaHistorica { get; set; }
        // % de referencia: lo mismo pero comparando contra días lejanos (15 a 30 días), sin relación entre sí
        public double TasaBase { get; set; }
        public int DiasAnalizados { get; set; }

        public List<RepeticionCandidato> Candidatos { get; set; } = new List<RepeticionCandidato>();
        public List<RepeticionHora> PorHoraDestino { get; set; } = new List<RepeticionHora>();
        public List<RepeticionHora> PorHoraOrigen { get; set; } = new List<RepeticionHora>();

        public List<string> HorasSorteo { get; set; } = new List<string>();
        public List<RepeticionPatron> Patrones { get; set; } = new List<RepeticionPatron>();
    }

    public class RepeticionPatron
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public List<string> Horas { get; set; } = new List<string>();
        // Veces que salieron todos sus animalitos dentro de la ventana de días
        public int Veces { get; set; }
        // Veces que salió en todo el histórico anterior a la fecha consultada
        public int VecesTotal { get; set; }
        // Días entre la fecha consultada y la última vez que salió (null = nunca)
        public int? DiasSinSalir { get; set; }
        public string UltimaFecha { get; set; }
        // Animalitos que marca el patrón para la fecha consultada (salieron en el día origen a esas horas)
        public List<RepeticionCandidato> Animalitos { get; set; } = new List<RepeticionCandidato>();
        public int Salieron => Animalitos.Count(a => a.YaSalio);
    }

    public class RepeticionHistorialFila
    {
        public string FechaOrigen { get; set; }
        // Día en que se completó el patrón
        public string Fecha { get; set; }
        // Días desde la vez anterior que salió (null = primera vez)
        public int? Dias { get; set; }
        // HorasOrigen = hora en el día origen, HoraHoy = hora(s) en que volvió a salir
        public List<RepeticionCandidato> Animalitos { get; set; } = new List<RepeticionCandidato>();
    }

    public class RepeticionCandidato
    {
        public int AnimalId { get; set; }
        public string Nombre { get; set; }
        public string ImageB64 { get; set; }
        public string HorasOrigen { get; set; }
        public bool YaSalio { get; set; }
        public string HoraHoy { get; set; }
    }

    public class RepeticionHora
    {
        public string Hora { get; set; }
        public int Total { get; set; }
        public int Aciertos { get; set; }
        public double Porcentaje => Total == 0 ? 0 : (double)Aciertos / Total;
    }
}
