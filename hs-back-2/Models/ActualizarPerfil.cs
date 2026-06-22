namespace proyecto_ids_api.Models
{
    public class ActualizarPerfilModel
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int Edad { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }
}