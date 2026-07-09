using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Models;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/fichas")]
public sealed class FichasController : ControllerBase
{
    private readonly string _connectionString;
    private readonly SessionService _sessions;

    public FichasController(IConfiguration configuration, SessionService sessions)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _sessions = sessions;
    }

    [HttpGet("mi-ficha")]
    public async Task<IActionResult> MiFicha(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "La sesión expiró." });
        if (usuario.Rol != "paciente" || usuario.PacienteId is null)
            return StatusCode(403, new { mensaje = "La ficha clínica solo está disponible para pacientes registrados." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var ficha = await LoadFichaAsync(conn, usuario.PacienteId.Value, cancellationToken);
        return ficha is null
            ? NotFound(new { mensaje = "No se encontró la ficha clínica." })
            : Ok(ficha);
    }

    [HttpGet("paciente/{pacienteId:int}")]
    public async Task<IActionResult> FichaPaciente(int pacienteId, CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "La sesión expiró." });
        if (usuario.Rol is not ("doctor" or "admin"))
            return StatusCode(403, new { mensaje = "No tienes permiso para ver esta ficha." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        if (usuario.Rol == "doctor")
        {
            const string relacionSql = """
                SELECT COUNT(*)
                FROM citas
                WHERE doctor_id = @doctorId AND paciente_id = @pacienteId;
                """;
            await using var relacion = new MySqlCommand(relacionSql, conn);
            relacion.Parameters.AddWithValue("@doctorId", usuario.DoctorId);
            relacion.Parameters.AddWithValue("@pacienteId", pacienteId);
            if (Convert.ToInt32(await relacion.ExecuteScalarAsync(cancellationToken)) == 0)
                return StatusCode(403, new { mensaje = "El paciente no pertenece a tu agenda." });
        }

        var ficha = await LoadFichaAsync(conn, pacienteId, cancellationToken);
        return ficha is null
            ? NotFound(new { mensaje = "No se encontró la ficha clínica." })
            : Ok(ficha);
    }

    [HttpPost("atenciones")]
    public async Task<IActionResult> RegistrarAtencion(
        RegistrarAtencionRequest model,
        CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (usuario is null) return Unauthorized(new { mensaje = "La sesión expiró." });
        if (usuario.Rol != "doctor" || usuario.DoctorId is null)
            return StatusCode(403, new { mensaje = "Solo un profesional puede registrar atenciones." });

        if (model.CitaId <= 0 || string.IsNullOrWhiteSpace(model.Diagnostico) || string.IsNullOrWhiteSpace(model.Tratamiento))
            return BadRequest(new { mensaje = "Selecciona una cita y completa diagnóstico y tratamiento." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            const string citaSql = """
                SELECT c.paciente_id, c.motivo, c.estado, fc.id AS ficha_id
                FROM citas c
                INNER JOIN fichas_clinicas fc ON fc.paciente_id = c.paciente_id
                WHERE c.id = @citaId AND c.doctor_id = @doctorId
                LIMIT 1;
                """;

            int pacienteId;
            int fichaId;
            string estado;
            string motivoCita;
            await using (var cita = new MySqlCommand(citaSql, conn, transaction))
            {
                cita.Parameters.AddWithValue("@citaId", model.CitaId);
                cita.Parameters.AddWithValue("@doctorId", usuario.DoctorId.Value);
                await using var reader = await cita.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound(new { mensaje = "La cita no existe o no pertenece a tu agenda." });
                }

                pacienteId = reader.GetInt32("paciente_id");
                fichaId = reader.GetInt32("ficha_id");
                estado = reader.GetString("estado");
                motivoCita = reader.IsDBNull(reader.GetOrdinal("motivo")) ? string.Empty : reader.GetString("motivo");
            }

            if (estado == "cancelada")
            {
                await transaction.RollbackAsync(cancellationToken);
                return BadRequest(new { mensaje = "No se puede registrar una atención para una cita cancelada." });
            }

            const string insertarSql = """
                INSERT INTO atenciones
                    (ficha_id, doctor_id, cita_id, fecha, motivo, diagnostico, tratamiento, receta, observaciones)
                VALUES
                    (@fichaId, @doctorId, @citaId, NOW(), @motivo, @diagnostico, @tratamiento, @receta, @observaciones);
                """;
            await using (var insertar = new MySqlCommand(insertarSql, conn, transaction))
            {
                insertar.Parameters.AddWithValue("@fichaId", fichaId);
                insertar.Parameters.AddWithValue("@doctorId", usuario.DoctorId.Value);
                insertar.Parameters.AddWithValue("@citaId", model.CitaId);
                insertar.Parameters.AddWithValue("@motivo", string.IsNullOrWhiteSpace(model.Motivo) ? motivoCita : model.Motivo.Trim());
                insertar.Parameters.AddWithValue("@diagnostico", model.Diagnostico.Trim());
                insertar.Parameters.AddWithValue("@tratamiento", model.Tratamiento.Trim());
                insertar.Parameters.AddWithValue("@receta", DbValue(model.Receta));
                insertar.Parameters.AddWithValue("@observaciones", DbValue(model.Observaciones));
                await insertar.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var actualizarCita = new MySqlCommand(
                "UPDATE citas SET estado = 'realizada' WHERE id = @citaId;",
                conn,
                transaction))
            {
                actualizarCita.Parameters.AddWithValue("@citaId", model.CitaId);
                await actualizarCita.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Ok(new { mensaje = "Atención registrada y cita marcada como realizada.", pacienteId });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { mensaje = "Esta cita ya tiene una atención clínica registrada." });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<object?> LoadFichaAsync(
        MySqlConnection conn,
        int pacienteId,
        CancellationToken cancellationToken)
    {
        const string fichaSql = """
            SELECT
                fc.id AS ficha_id,
                fc.observaciones_generales,
                fc.creada_en,
                p.id AS paciente_id,
                p.fecha_nacimiento,
                p.prevision,
                p.alergias,
                p.antecedentes,
                u.nombre,
                u.rut
            FROM fichas_clinicas fc
            INNER JOIN pacientes p ON p.id = fc.paciente_id
            INNER JOIN usuarios u ON u.id = p.usuario_id
            WHERE p.id = @pacienteId
            LIMIT 1;
            """;

        int fichaId;
        object paciente;
        string? observacionesGenerales;
        DateTime creadaEn;
        await using (var cmd = new MySqlCommand(fichaSql, conn))
        {
            cmd.Parameters.AddWithValue("@pacienteId", pacienteId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;

            fichaId = reader.GetInt32("ficha_id");
            observacionesGenerales = ReadNullableString(reader, "observaciones_generales");
            creadaEn = reader.GetDateTime("creada_en");
            paciente = new
            {
                id = reader.GetInt32("paciente_id"),
                nombre = reader.GetString("nombre"),
                rut = reader.GetString("rut"),
                fechaNacimiento = reader.IsDBNull(reader.GetOrdinal("fecha_nacimiento"))
                    ? (DateTime?)null
                    : reader.GetDateTime("fecha_nacimiento"),
                prevision = ReadNullableString(reader, "prevision"),
                alergias = ReadNullableString(reader, "alergias"),
                antecedentes = ReadNullableString(reader, "antecedentes")
            };
        }

        const string atencionesSql = """
            SELECT
                a.id,
                a.fecha,
                a.motivo,
                a.diagnostico,
                a.tratamiento,
                a.receta,
                a.observaciones,
                u.nombre AS doctor,
                d.especialidad
            FROM atenciones a
            INNER JOIN doctores d ON d.id = a.doctor_id
            INNER JOIN usuarios u ON u.id = d.usuario_id
            WHERE a.ficha_id = @fichaId
            ORDER BY a.fecha DESC;
            """;

        var atenciones = new List<object>();
        await using (var cmd = new MySqlCommand(atencionesSql, conn))
        {
            cmd.Parameters.AddWithValue("@fichaId", fichaId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                atenciones.Add(new
                {
                    id = reader.GetInt32("id"),
                    fecha = reader.GetDateTime("fecha"),
                    motivo = reader.GetString("motivo"),
                    diagnostico = ReadNullableString(reader, "diagnostico"),
                    tratamiento = ReadNullableString(reader, "tratamiento"),
                    receta = ReadNullableString(reader, "receta"),
                    observaciones = ReadNullableString(reader, "observaciones"),
                    doctor = reader.GetString("doctor"),
                    especialidad = reader.GetString("especialidad")
                });
            }
        }

        return new
        {
            id = fichaId,
            observacionesGenerales,
            creadaEn,
            paciente,
            atenciones
        };
    }

    private static string? ReadNullableString(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(column);

    private static object DbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
