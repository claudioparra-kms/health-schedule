namespace proyecto_ids_api.Models;

public sealed record SessionUser(
    int UsuarioId,
    string Nombre,
    string Rut,
    string? Correo,
    string Rol,
    int? PacienteId,
    int? DoctorId,
    string? Especialidad);
