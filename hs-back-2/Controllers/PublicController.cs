using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController : ControllerBase
{
    private readonly string _connectionString;

    public PublicController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
    }

    [HttpGet("especialidades")]
    public async Task<IActionResult> Especialidades(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT d.especialidad
            FROM doctores d
            INNER JOIN usuarios u ON u.id = d.usuario_id
            WHERE u.activo = TRUE
            ORDER BY d.especialidad;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var especialidades = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            especialidades.Add(reader.GetString("especialidad"));

        return Ok(especialidades);
    }

    [HttpGet("doctores")]
    public async Task<IActionResult> Doctores(
        [FromQuery] string? especialidad,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT d.id, u.nombre, d.especialidad, d.numero_registro
            FROM doctores d
            INNER JOIN usuarios u ON u.id = d.usuario_id
            WHERE u.activo = TRUE
              AND (@especialidad IS NULL OR @especialidad = '' OR d.especialidad = @especialidad)
            ORDER BY d.especialidad, u.nombre;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@especialidad", especialidad?.Trim() ?? string.Empty);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var doctores = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            doctores.Add(new
            {
                id = reader.GetInt32("id"),
                nombre = reader.GetString("nombre"),
                especialidad = reader.GetString("especialidad"),
                numeroRegistro = reader.IsDBNull(reader.GetOrdinal("numero_registro"))
                    ? null
                    : reader.GetString("numero_registro")
            });
        }

        return Ok(doctores);
    }

    [HttpGet("horarios")]
    public async Task<IActionResult> Horarios(
        [FromQuery] int doctorId,
        [FromQuery] DateTime fecha,
        CancellationToken cancellationToken)
    {
        if (doctorId <= 0)
            return BadRequest(new { mensaje = "Selecciona un profesional." });

        var dia = fecha.Date;
        if (dia < DateTime.Today)
            return Ok(Array.Empty<object>());
        if (dia > DateTime.Today.AddDays(90))
            return BadRequest(new { mensaje = "Solo se puede agendar dentro de los próximos 90 días." });

        var diaSemana = ((int)dia.DayOfWeek + 6) % 7 + 1;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string disponibilidadSql = """
            SELECT dd.hora_inicio, dd.hora_fin, dd.duracion_bloque_min
            FROM disponibilidad_doctor dd
            INNER JOIN doctores d ON d.id = dd.doctor_id
            INNER JOIN usuarios u ON u.id = d.usuario_id
            WHERE dd.doctor_id = @doctorId
              AND dd.dia_semana = @diaSemana
              AND dd.activo = TRUE
              AND u.activo = TRUE
            ORDER BY dd.hora_inicio;
            """;

        var bloques = new List<(TimeSpan Inicio, TimeSpan Fin, int Duracion)>();
        await using (var cmd = new MySqlCommand(disponibilidadSql, conn))
        {
            cmd.Parameters.AddWithValue("@doctorId", doctorId);
            cmd.Parameters.AddWithValue("@diaSemana", diaSemana);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                bloques.Add((
                    (TimeSpan)reader["hora_inicio"],
                    (TimeSpan)reader["hora_fin"],
                    reader.GetInt32("duracion_bloque_min")));
            }
        }

        if (bloques.Count == 0) return Ok(Array.Empty<object>());

        var inicioDia = dia;
        var finDia = dia.AddDays(1);
        var ocupados = new List<(DateTime Inicio, DateTime Fin)>();

        const string ocupadosSql = """
            SELECT fecha_inicio, fecha_fin
            FROM citas
            WHERE doctor_id = @doctorId
              AND estado IN ('pendiente', 'confirmada')
              AND fecha_inicio >= @inicioDia
              AND fecha_inicio < @finDia;
            """;
        await using (var cmd = new MySqlCommand(ocupadosSql, conn))
        {
            cmd.Parameters.AddWithValue("@doctorId", doctorId);
            cmd.Parameters.AddWithValue("@inicioDia", inicioDia);
            cmd.Parameters.AddWithValue("@finDia", finDia);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                ocupados.Add((reader.GetDateTime("fecha_inicio"), reader.GetDateTime("fecha_fin")));
        }

        const string bloqueosSql = """
            SELECT inicio, fin
            FROM bloqueos_agenda
            WHERE doctor_id = @doctorId
              AND inicio < @finDia
              AND fin > @inicioDia;
            """;
        await using (var cmd = new MySqlCommand(bloqueosSql, conn))
        {
            cmd.Parameters.AddWithValue("@doctorId", doctorId);
            cmd.Parameters.AddWithValue("@inicioDia", inicioDia);
            cmd.Parameters.AddWithValue("@finDia", finDia);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                ocupados.Add((reader.GetDateTime("inicio"), reader.GetDateTime("fin")));
        }

        var ahora = DateTime.Now;
        var horarios = new List<object>();
        foreach (var bloque in bloques)
        {
            var cursor = dia.Add(bloque.Inicio);
            var limite = dia.Add(bloque.Fin);

            while (cursor.AddMinutes(bloque.Duracion) <= limite)
            {
                var fin = cursor.AddMinutes(bloque.Duracion);
                var choca = ocupados.Any(o => cursor < o.Fin && fin > o.Inicio);
                if (!choca && cursor > ahora.AddMinutes(30))
                {
                    horarios.Add(new
                    {
                        hora = cursor.ToString("HH:mm"),
                        fechaInicio = cursor,
                        duracionMinutos = bloque.Duracion
                    });
                }

                cursor = fin;
            }
        }

        return Ok(horarios);
    }
}
