namespace proyecto_ids_api.Models
{
    public class CrearCitaModel
    {
        public int PacienteId { get; set; }
        public int DoctorId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}