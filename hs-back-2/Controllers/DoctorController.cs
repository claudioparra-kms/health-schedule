using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/doctor")]
public sealed class DoctorController : ControllerBase
{
    private readonly string _connectionString;
    private readonly SessionService _sessions;

    public DoctorController(IConfiguration configuration, SessionService sessions)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _sessions = sessions;
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "La sesión expiró." });
        if (usuario.Rol != "doctor" || usuario.DoctorId is null)
            return StatusCode(403, new { mensaje = "Acceso exclusivo para profesionales." });

        const string sql = """
            SELECT
                SUM(CASE WHEN DATE(fecha_inicio) = CURDATE() AND estado IN ('pendiente','confirmada') THEN 1 ELSE 0 END) AS citas_hoy,
                SUM(CASE WHEN fecha_inicio > NOW() AND estado IN ('pendiente','confirmada') THEN 1 ELSE 0 END) AS proximas,
                SUM(CASE WHEN estado = 'pendiente' THEN 1 ELSE 0 END) AS pendientes,
                COUNT(DISTINCT paciente_id) AS pacientes
            FROM citas
            WHERE doctor_id = @doctorId;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@doctorId", usuario.DoctorId.Value);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return Ok(new
        {
            citasHoy = GetInt(reader, "citas_hoy"),
            proximas = GetInt(reader, "proximas"),
            pendientes = GetInt(reader, "pendientes"),
            pacientes = GetInt(reader, "pacientes")
        });
    }

    [HttpGet("pacientes")]
    public async Task<IActionResult> Pacientes(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "La sesión expiró." });
        if (usuario.Rol != "doctor" || usuario.DoctorId is null)
            return StatusCode(403, new { mensaje = "Acceso exclusivo para profesionales." });

        const string sql = """
            SELECT
                p.id,
                u.nombre,
                u.rut,
                u.correo,
                u.telefono,
                p.prevision,
                p.alergias,
                MAX(CASE WHEN c.fecha_inicio < NOW() OR c.estado = 'realizada' THEN c.fecha_inicio END) AS ultima_cita,
                MIN(CASE WHEN c.fecha_inicio >= NOW() AND c.estado IN ('pendiente','confirmada') THEN c.fecha_inicio END) AS proxima_cita,
                COUNT(c.id) AS total_citas
            FROM citas c
            INNER JOIN pacientes p ON p.id = c.paciente_id
            INNER JOIN usuarios u ON u.id = p.usuario_id
            WHERE c.doctor_id = @doctorId
            GROUP BY p.id, u.nombre, u.rut, u.correo, u.telefono, p.prevision, p.alergias
            ORDER BY u.nombre;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@doctorId", usuario.DoctorId.Value);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var pacientes = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            pacientes.Add(new
            {
                id = reader.GetInt32("id"),
                nombre = reader.GetString("nombre"),
                rut = reader.GetString("rut"),
                correo = ReadNullableString(reader, "correo"),
                telefono = ReadNullableString(reader, "telefono"),
                prevision = ReadNullableString(reader, "prevision"),
                alergias = ReadNullableString(reader, "alergias"),
                ultimaCita = reader.IsDBNull(reader.GetOrdinal("ultima_cita")) ? (DateTime?)null : reader.GetDateTime("ultima_cita"),
                proximaCita = reader.IsDBNull(reader.GetOrdinal("proxima_cita")) ? (DateTime?)null : reader.GetDateTime("proxima_cita"),
                totalCitas = reader.GetInt32("total_citas")
            });
        }

        return Ok(pacientes);
    }

    private static int GetInt(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? 0 : Convert.ToInt32(reader[column]);

    private static string? ReadNullableString(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(column);
}
