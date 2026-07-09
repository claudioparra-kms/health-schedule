using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Models;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/citas")]
public sealed class CitasController : ControllerBase
{
    private readonly string _connectionString;
    private readonly SessionService _sessions;

    public CitasController(IConfiguration configuration, SessionService sessions)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _sessions = sessions;
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearCitaRequest model, CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "Debes iniciar sesión." });
        if (usuario.Rol is not ("paciente" or "invitado") || usuario.PacienteId is null)
            return StatusCode(403, new { mensaje = "Solo pacientes e invitados pueden reservar horas." });

        if (model.DoctorId <= 0 || model.FechaInicio == default)
            return BadRequest(new { mensaje = "Selecciona profesional, fecha y horario." });
        if (model.FechaInicio <= DateTime.Now.AddMinutes(30))
            return BadRequest(new { mensaje = "La hora debe reservarse con al menos 30 minutos de anticipación." });
        if (model.FechaInicio > DateTime.Now.AddDays(90))
            return BadRequest(new { mensaje = "Solo puedes reservar dentro de los próximos 90 días." });

        var motivo = string.IsNullOrWhiteSpace(model.Motivo)
            ? "Consulta general"
            : model.Motivo.Trim();
        if (motivo.Length > 500)
            return BadRequest(new { mensaje = "El motivo es demasiado largo." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var diaSemana = ((int)model.FechaInicio.DayOfWeek + 6) % 7 + 1;
            const string disponibilidadSql = """
                SELECT dd.duracion_bloque_min, dd.hora_inicio, dd.hora_fin
                FROM disponibilidad_doctor dd
                INNER JOIN doctores d ON d.id = dd.doctor_id
                INNER JOIN usuarios u ON u.id = d.usuario_id
                WHERE dd.doctor_id = @doctorId
                  AND dd.dia_semana = @diaSemana
                  AND dd.activo = TRUE
                  AND u.activo = TRUE
                  AND TIME(@fechaInicio) >= dd.hora_inicio
                  AND TIME(@fechaInicio) < dd.hora_fin
                ORDER BY dd.hora_inicio
                LIMIT 1;
                """;

            int duracion;
            TimeSpan horaInicioBloque;
            TimeSpan horaFinBloque;
            await using (var disponibilidad = new MySqlCommand(disponibilidadSql, conn, transaction))
            {
                disponibilidad.Parameters.AddWithValue("@doctorId", model.DoctorId);
                disponibilidad.Parameters.AddWithValue("@diaSemana", diaSemana);
                disponibilidad.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
                await using var reader = await disponibilidad.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { mensaje = "El profesional no atiende en ese horario." });
                }

                duracion = reader.GetInt32("duracion_bloque_min");
                horaInicioBloque = (TimeSpan)reader["hora_inicio"];
                horaFinBloque = (TimeSpan)reader["hora_fin"];
            }

            var diferenciaDesdeInicio = model.FechaInicio.TimeOfDay - horaInicioBloque;
            var duracionTicks = TimeSpan.FromMinutes(duracion).Ticks;
            if (diferenciaDesdeInicio.Ticks < 0 ||
                diferenciaDesdeInicio.Ticks % duracionTicks != 0 ||
                model.FechaInicio.Second != 0 ||
                model.FechaInicio.Millisecond != 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(new { mensaje = "Selecciona uno de los bloques horarios ofrecidos por el sistema." });
            }

            var fechaFin = model.FechaInicio.AddMinutes(duracion);
            if (fechaFin.TimeOfDay > horaFinBloque)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(new { mensaje = "El bloque seleccionado queda fuera del horario del profesional." });
            }

            const string conflictoSql = """
                SELECT COUNT(*)
                FROM citas
                WHERE estado IN ('pendiente', 'confirmada')
                  AND (doctor_id = @doctorId OR paciente_id = @pacienteId)
                  AND @fechaInicio < fecha_fin
                  AND @fechaFin > fecha_inicio;
                """;
            await using (var conflicto = new MySqlCommand(conflictoSql, conn, transaction))
            {
                conflicto.Parameters.AddWithValue("@doctorId", model.DoctorId);
                conflicto.Parameters.AddWithValue("@pacienteId", usuario.PacienteId.Value);
                conflicto.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
                conflicto.Parameters.AddWithValue("@fechaFin", fechaFin);
                if (Convert.ToInt32(await conflicto.ExecuteScalarAsync(cancellationToken)) > 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Conflict(new { mensaje = "Ese horario ya no está disponible o coincide con otra cita tuya." });
                }
            }

            const string bloqueoSql = """
                SELECT COUNT(*)
                FROM bloqueos_agenda
                WHERE doctor_id = @doctorId
                  AND @fechaInicio < fin
                  AND @fechaFin > inicio;
                """;
            await using (var bloqueo = new MySqlCommand(bloqueoSql, conn, transaction))
            {
                bloqueo.Parameters.AddWithValue("@doctorId", model.DoctorId);
                bloqueo.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
                bloqueo.Parameters.AddWithValue("@fechaFin", fechaFin);
                if (Convert.ToInt32(await bloqueo.ExecuteScalarAsync(cancellationToken)) > 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Conflict(new { mensaje = "La agenda del profesional está bloqueada en ese horario." });
                }
            }

            const string insertarSql = """
                INSERT INTO citas (paciente_id, doctor_id, fecha_inicio, fecha_fin, motivo, estado)
                VALUES (@pacienteId, @doctorId, @fechaInicio, @fechaFin, @motivo, 'pendiente');
                SELECT LAST_INSERT_ID();
                """;
            await using var insertar = new MySqlCommand(insertarSql, conn, transaction);
            insertar.Parameters.AddWithValue("@pacienteId", usuario.PacienteId.Value);
            insertar.Parameters.AddWithValue("@doctorId", model.DoctorId);
            insertar.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
            insertar.Parameters.AddWithValue("@fechaFin", fechaFin);
            insertar.Parameters.AddWithValue("@motivo", motivo);
            var citaId = Convert.ToInt32(await insertar.ExecuteScalarAsync(cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return Ok(new
            {
                mensaje = "Hora reservada correctamente.",
                citaId,
                fechaInicio = model.FechaInicio,
                fechaFin
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { mensaje = "Ese horario acaba de ser reservado. Selecciona otro." });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpGet("mias")]
    public async Task<IActionResult> Mias(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "Debes iniciar sesión." });
        if (usuario.PacienteId is null)
            return StatusCode(403, new { mensaje = "Esta cuenta no corresponde a un paciente." });

        const string sql = """
            SELECT
                c.id,
                c.fecha_inicio,
                c.fecha_fin,
                c.estado,
                c.motivo,
                d.id AS doctor_id,
                u.nombre AS doctor,
                d.especialidad
            FROM citas c
            INNER JOIN doctores d ON d.id = c.doctor_id
            INNER JOIN usuarios u ON u.id = d.usuario_id
            WHERE c.paciente_id = @pacienteId
            ORDER BY c.fecha_inicio DESC;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pacienteId", usuario.PacienteId.Value);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        return Ok(await ReadAppointmentsAsync(reader, includePatient: false, cancellationToken));
    }

    [HttpGet("agenda-doctor")]
    public async Task<IActionResult> AgendaDoctor(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "Debes iniciar sesión." });
        if (usuario.Rol != "doctor" || usuario.DoctorId is null)
            return StatusCode(403, new { mensaje = "Acceso exclusivo para profesionales." });

        const string sql = """
            SELECT
                c.id,
                c.fecha_inicio,
                c.fecha_fin,
                c.estado,
                c.motivo,
                p.id AS paciente_id,
                u.nombre AS paciente,
                u.rut AS rut_paciente
            FROM citas c
            INNER JOIN pacientes p ON p.id = c.paciente_id
            INNER JOIN usuarios u ON u.id = p.usuario_id
            WHERE c.doctor_id = @doctorId
            ORDER BY c.fecha_inicio DESC;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@doctorId", usuario.DoctorId.Value);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        return Ok(await ReadAppointmentsAsync(reader, includePatient: true, cancellationToken));
    }

    [HttpPatch("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "Debes iniciar sesión." });
        if (usuario.PacienteId is null)
            return StatusCode(403, new { mensaje = "Solo el paciente puede cancelar esta cita." });

        const string sql = """
            UPDATE citas
            SET estado = 'cancelada'
            WHERE id = @id
              AND paciente_id = @pacienteId
              AND estado IN ('pendiente', 'confirmada')
              AND fecha_inicio > NOW();
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@pacienteId", usuario.PacienteId.Value);
        var filas = await cmd.ExecuteNonQueryAsync(cancellationToken);

        return filas == 0
            ? BadRequest(new { mensaje = "La cita no existe, ya fue cancelada o ya ocurrió." })
            : Ok(new { mensaje = "Cita cancelada correctamente." });
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(
        int id,
        CambiarEstadoCitaRequest model,
        CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "Debes iniciar sesión." });
        if (usuario.Rol is not ("doctor" or "admin"))
            return StatusCode(403, new { mensaje = "No tienes permiso para cambiar el estado." });

        var estado = model.Estado.Trim().ToLowerInvariant();
        string[] permitidos = ["pendiente", "confirmada", "realizada", "cancelada", "no_asiste"];
        if (!permitidos.Contains(estado))
            return BadRequest(new { mensaje = "Estado de cita inválido." });

        var filtroDoctor = usuario.Rol == "doctor" ? " AND doctor_id = @doctorId" : string.Empty;
        var sql = $"UPDATE citas SET estado = @estado WHERE id = @id{filtroDoctor};";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@estado", estado);
        cmd.Parameters.AddWithValue("@id", id);
        if (usuario.Rol == "doctor") cmd.Parameters.AddWithValue("@doctorId", usuario.DoctorId);
        var filas = await cmd.ExecuteNonQueryAsync(cancellationToken);

        return filas == 0
            ? NotFound(new { mensaje = "Cita no encontrada." })
            : Ok(new { mensaje = "Estado actualizado correctamente." });
    }

    private static async Task<List<object>> ReadAppointmentsAsync(
        MySqlDataReader reader,
        bool includePatient,
        CancellationToken cancellationToken)
    {
        var citas = new List<object>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (includePatient)
            {
                citas.Add(new
                {
                    id = reader.GetInt32("id"),
                    fechaInicio = reader.GetDateTime("fecha_inicio"),
                    fechaFin = reader.GetDateTime("fecha_fin"),
                    estado = reader.GetString("estado"),
                    motivo = reader.IsDBNull(reader.GetOrdinal("motivo")) ? string.Empty : reader.GetString("motivo"),
                    pacienteId = reader.GetInt32("paciente_id"),
                    paciente = reader.GetString("paciente"),
                    rutPaciente = reader.GetString("rut_paciente")
                });
            }
            else
            {
                citas.Add(new
                {
                    id = reader.GetInt32("id"),
                    fechaInicio = reader.GetDateTime("fecha_inicio"),
                    fechaFin = reader.GetDateTime("fecha_fin"),
                    estado = reader.GetString("estado"),
                    motivo = reader.IsDBNull(reader.GetOrdinal("motivo")) ? string.Empty : reader.GetString("motivo"),
                    doctorId = reader.GetInt32("doctor_id"),
                    doctor = reader.GetString("doctor"),
                    especialidad = reader.GetString("especialidad")
                });
            }
        }

        return citas;
    }
}
