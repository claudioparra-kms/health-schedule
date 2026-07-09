using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Models;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly string _connectionString;
    private readonly SessionService _sessions;

    public AdminController(IConfiguration configuration, SessionService sessions)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _sessions = sessions;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM usuarios WHERE activo = TRUE) AS usuarios,
                (SELECT COUNT(*) FROM pacientes p INNER JOIN usuarios u ON u.id = p.usuario_id WHERE u.activo = TRUE) AS pacientes,
                (SELECT COUNT(*) FROM doctores d INNER JOIN usuarios u ON u.id = d.usuario_id WHERE u.activo = TRUE) AS doctores,
                (SELECT COUNT(*) FROM citas WHERE DATE(fecha_inicio) = CURDATE()) AS citas_hoy,
                (SELECT COUNT(*) FROM citas WHERE estado = 'pendiente') AS citas_pendientes;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return Ok(new
        {
            usuarios = Convert.ToInt32(reader["usuarios"]),
            pacientes = Convert.ToInt32(reader["pacientes"]),
            doctores = Convert.ToInt32(reader["doctores"]),
            citasHoy = Convert.ToInt32(reader["citas_hoy"]),
            citasPendientes = Convert.ToInt32(reader["citas_pendientes"])
        });
    }

    [HttpGet("usuarios")]
    public async Task<IActionResult> Usuarios(CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        const string sql = """
            SELECT u.id, u.rut, u.nombre, u.correo, u.telefono, r.nombre AS rol, u.activo, u.creado_en
            FROM usuarios u
            INNER JOIN roles r ON r.id = u.rol_id
            ORDER BY u.creado_en DESC, u.nombre;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var usuarios = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            usuarios.Add(new
            {
                id = reader.GetInt32("id"),
                rut = reader.GetString("rut"),
                nombre = reader.GetString("nombre"),
                correo = ReadNullableString(reader, "correo"),
                telefono = ReadNullableString(reader, "telefono"),
                rol = reader.GetString("rol"),
                activo = reader.GetBoolean("activo"),
                creadoEn = reader.GetDateTime("creado_en")
            });
        }

        return Ok(usuarios);
    }

    [HttpGet("citas")]
    public async Task<IActionResult> Citas(CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        const string sql = """
            SELECT
                c.id,
                c.fecha_inicio,
                c.fecha_fin,
                c.estado,
                c.motivo,
                up.nombre AS paciente,
                up.rut AS rut_paciente,
                ud.nombre AS doctor,
                d.especialidad
            FROM citas c
            INNER JOIN pacientes p ON p.id = c.paciente_id
            INNER JOIN usuarios up ON up.id = p.usuario_id
            INNER JOIN doctores d ON d.id = c.doctor_id
            INNER JOIN usuarios ud ON ud.id = d.usuario_id
            ORDER BY c.fecha_inicio DESC
            LIMIT 200;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var citas = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            citas.Add(new
            {
                id = reader.GetInt32("id"),
                fechaInicio = reader.GetDateTime("fecha_inicio"),
                fechaFin = reader.GetDateTime("fecha_fin"),
                estado = reader.GetString("estado"),
                motivo = ReadNullableString(reader, "motivo"),
                paciente = reader.GetString("paciente"),
                rutPaciente = reader.GetString("rut_paciente"),
                doctor = reader.GetString("doctor"),
                especialidad = reader.GetString("especialidad")
            });
        }

        return Ok(citas);
    }

    [HttpPatch("usuarios/{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoUsuario(
        int id,
        CambiarEstadoUsuarioRequest model,
        CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;
        if (admin.User!.UsuarioId == id && !model.Activo)
            return BadRequest(new { mensaje = "No puedes desactivar tu propia cuenta." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        await using var cmd = new MySqlCommand(
            "UPDATE usuarios SET activo = @activo WHERE id = @id;",
            conn,
            transaction);
        cmd.Parameters.AddWithValue("@activo", model.Activo);
        cmd.Parameters.AddWithValue("@id", id);
        var filas = await cmd.ExecuteNonQueryAsync(cancellationToken);

        if (filas == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        if (!model.Activo)
            await _sessions.RevokeAllForUserAsync(conn, transaction, id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Ok(new { mensaje = model.Activo ? "Usuario activado." : "Usuario desactivado." });
    }

    private async Task<(SessionUser? User, IActionResult? Error)> RequireAdminAsync(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null)
            return (null, Unauthorized(new { mensaje = "La sesión expiró." }));
        if (usuario.Rol != "admin")
            return (usuario, StatusCode(403, new { mensaje = "Acceso exclusivo para administración." }));
        return (usuario, null);
    }

    private static string? ReadNullableString(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(column);
}
