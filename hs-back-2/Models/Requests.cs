using System.ComponentModel.DataAnnotations;

namespace proyecto_ids_api.Models;

public sealed class LoginRequest
{
    [Required] public string Rut { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public sealed class InvitadoRequest
{
    [Required] public string Rut { get; set; } = string.Empty;
}

public sealed class RegistroRequest
{
    [Required] public string Rut { get; set; } = string.Empty;
    [Required] public string Nombre { get; set; } = string.Empty;
    [Required, EmailAddress] public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
}

public sealed class RecuperarPasswordRequest
{
    [Required] public string RutOCorreo { get; set; } = string.Empty;
}

public sealed class ActualizarPerfilRequest
{
    [Required, EmailAddress] public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Prevision { get; set; } = string.Empty;
    public string Alergias { get; set; } = string.Empty;
    public string Antecedentes { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}

public sealed class CrearCitaRequest
{
    public int DoctorId { get; set; }
    public DateTime FechaInicio { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class CambiarEstadoCitaRequest
{
    [Required] public string Estado { get; set; } = string.Empty;
}

public sealed class RegistrarAtencionRequest
{
    public int CitaId { get; set; }
    [Required] public string Motivo { get; set; } = string.Empty;
    [Required] public string Diagnostico { get; set; } = string.Empty;
    [Required] public string Tratamiento { get; set; } = string.Empty;
    public string Receta { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}

public sealed class CambiarEstadoUsuarioRequest
{
    public bool Activo { get; set; }
}
