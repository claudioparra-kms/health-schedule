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
    private readonly PasswordHasher _passwordHasher;

    public AdminController(
        IConfiguration configuration,
        SessionService sessions,
        PasswordHasher passwordHasher)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _sessions = sessions;
        _passwordHasher = passwordHasher;
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

    [HttpPost("usuarios")]
    public async Task<IActionResult> CrearUsuario(
        CrearUsuarioAdminRequest model,
        CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        var rut = RutValidator.Normalize(model.Rut);
        var nombre = model.Nombre.Trim();
        var correo = string.IsNullOrWhiteSpace(model.Correo)
            ? null
            : model.Correo.Trim().ToLowerInvariant();
        var telefono = model.Telefono.Trim();
        var rol = model.Rol.Trim().ToLowerInvariant();
        var password = model.Password.Trim();

        var rolesPermitidos = new[] { "paciente", "doctor", "admin" };
        if (!rolesPermitidos.Contains(rol))
            return BadRequest(new { mensaje = "El rol seleccionado no es válido para creación administrativa." });
        if (!RutValidator.IsValid(rut))
            return BadRequest(new { mensaje = "Ingresa un RUT chileno válido." });
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { mensaje = "El nombre del usuario es obligatorio." });
        if (string.IsNullOrWhiteSpace(correo))
            return BadRequest(new { mensaje = "El correo institucional o de contacto es obligatorio." });
        if (password.Length < 8)
            return BadRequest(new { mensaje = "La contraseña inicial debe tener al menos 8 caracteres." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            int rolId;
            await using (var obtenerRol = new MySqlCommand(
                "SELECT id FROM roles WHERE nombre = @rol LIMIT 1;",
                conn,
                transaction))
            {
                obtenerRol.Parameters.AddWithValue("@rol", rol);
                var rolIdObj = await obtenerRol.ExecuteScalarAsync(cancellationToken);
                if (rolIdObj is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { mensaje = "Rol no disponible en la base de datos." });
                }

                rolId = Convert.ToInt32(rolIdObj);
            }

            var passwordHash = _passwordHasher.Hash(password);
            int usuarioId;
            await using (var insertarUsuario = new MySqlCommand(
                """
                INSERT INTO usuarios (rut, nombre, correo, password_hash, telefono, rol_id, activo)
                VALUES (@rut, @nombre, @correo, @passwordHash, @telefono, @rolId, TRUE);
                SELECT LAST_INSERT_ID();
                """,
                conn,
                transaction))
            {
                insertarUsuario.Parameters.AddWithValue("@rut", rut);
                insertarUsuario.Parameters.AddWithValue("@nombre", nombre);
                insertarUsuario.Parameters.AddWithValue("@correo", correo);
                insertarUsuario.Parameters.AddWithValue("@passwordHash", passwordHash);
                insertarUsuario.Parameters.AddWithValue("@telefono", DbValue(telefono));
                insertarUsuario.Parameters.AddWithValue("@rolId", rolId);
                usuarioId = Convert.ToInt32(await insertarUsuario.ExecuteScalarAsync(cancellationToken));
            }

            if (rol == "paciente")
            {
                int pacienteId;
                await using (var insertarPaciente = new MySqlCommand(
                    """
                    INSERT INTO pacientes (usuario_id, fecha_nacimiento, direccion, prevision, alergias, antecedentes)
                    VALUES (@usuarioId, @fechaNacimiento, @direccion, @prevision, @alergias, @antecedentes);
                    SELECT LAST_INSERT_ID();
                    """,
                    conn,
                    transaction))
                {
                    insertarPaciente.Parameters.AddWithValue("@usuarioId", usuarioId);
                    insertarPaciente.Parameters.AddWithValue(
                        "@fechaNacimiento",
                        model.FechaNacimiento.HasValue ? model.FechaNacimiento.Value.Date : DBNull.Value);
                    insertarPaciente.Parameters.AddWithValue("@direccion", DbValue(model.Direccion));
                    insertarPaciente.Parameters.AddWithValue("@prevision", DbValue(model.Prevision));
                    insertarPaciente.Parameters.AddWithValue("@alergias", DbValue(model.Alergias));
                    insertarPaciente.Parameters.AddWithValue("@antecedentes", DbValue(model.Antecedentes));
                    pacienteId = Convert.ToInt32(await insertarPaciente.ExecuteScalarAsync(cancellationToken));
                }

                await using var crearFicha = new MySqlCommand(
                    """
                    INSERT INTO fichas_clinicas (paciente_id, observaciones_generales)
                    VALUES (@pacienteId, 'Ficha clínica creada por administración.');
                    """,
                    conn,
                    transaction);
                crearFicha.Parameters.AddWithValue("@pacienteId", pacienteId);
                await crearFicha.ExecuteNonQueryAsync(cancellationToken);
            }

            if (rol == "doctor")
            {
                var especialidad = model.Especialidad.Trim();
                var numeroRegistro = model.NumeroRegistro.Trim();
                if (string.IsNullOrWhiteSpace(especialidad) || string.IsNullOrWhiteSpace(numeroRegistro))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { mensaje = "La especialidad y el número de registro son obligatorios para crear profesionales." });
                }

                int doctorId;
                await using (var insertarDoctor = new MySqlCommand(
                    """
                    INSERT INTO doctores (usuario_id, especialidad, numero_registro)
                    VALUES (@usuarioId, @especialidad, @numeroRegistro);
                    SELECT LAST_INSERT_ID();
                    """,
                    conn,
                    transaction))
                {
                    insertarDoctor.Parameters.AddWithValue("@usuarioId", usuarioId);
                    insertarDoctor.Parameters.AddWithValue("@especialidad", especialidad);
                    insertarDoctor.Parameters.AddWithValue("@numeroRegistro", numeroRegistro);
                    doctorId = Convert.ToInt32(await insertarDoctor.ExecuteScalarAsync(cancellationToken));
                }

                await CrearDisponibilidadProfesionalAsync(conn, transaction, doctorId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var usuarioCreado = await LeerUsuarioDetalleAsync(conn, usuarioId, cancellationToken);
            return Created($"/api/admin/usuarios/{usuarioId}", new
            {
                mensaje = rol == "doctor"
                    ? "Profesional creado correctamente. Se asignó disponibilidad inicial de lunes a viernes."
                    : "Usuario creado correctamente.",
                usuario = usuarioCreado
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { mensaje = "El RUT, correo o número de registro ya está asociado a otro usuario." });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpGet("usuarios/{id:int}")]
    public async Task<IActionResult> ObtenerUsuario(int id, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        const string sql = """
            SELECT
                u.id,
                u.rut,
                u.nombre,
                u.correo,
                u.telefono,
                r.nombre AS rol,
                u.activo,
                u.creado_en,
                p.fecha_nacimiento,
                p.direccion,
                p.prevision,
                p.alergias,
                p.antecedentes,
                d.especialidad,
                d.numero_registro
            FROM usuarios u
            INNER JOIN roles r ON r.id = u.rol_id
            LEFT JOIN pacientes p ON p.usuario_id = u.id
            LEFT JOIN doctores d ON d.usuario_id = u.id
            WHERE u.id = @id
            LIMIT 1;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return NotFound(new { mensaje = "Usuario no encontrado." });

        return Ok(MapUsuarioDetalle(reader));
    }

    [HttpPut("usuarios/{id:int}")]
    public async Task<IActionResult> ActualizarUsuario(
        int id,
        ActualizarUsuarioAdminRequest model,
        CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        var rut = RutValidator.Normalize(model.Rut);
        var nombre = model.Nombre.Trim();
        var correo = string.IsNullOrWhiteSpace(model.Correo)
            ? null
            : model.Correo.Trim().ToLowerInvariant();
        var telefono = model.Telefono.Trim();

        if (!RutValidator.IsValid(rut))
            return BadRequest(new { mensaje = "Ingresa un RUT chileno válido." });
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { mensaje = "El nombre del usuario es obligatorio." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string? rol;
            await using (var obtenerRol = new MySqlCommand(
                """
                SELECT r.nombre
                FROM usuarios u
                INNER JOIN roles r ON r.id = u.rol_id
                WHERE u.id = @id
                LIMIT 1;
                """,
                conn,
                transaction))
            {
                obtenerRol.Parameters.AddWithValue("@id", id);
                rol = Convert.ToString(await obtenerRol.ExecuteScalarAsync(cancellationToken));
            }

            if (rol is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return NotFound(new { mensaje = "Usuario no encontrado." });
            }

            await using (var actualizarUsuario = new MySqlCommand(
                """
                UPDATE usuarios
                SET rut = @rut,
                    nombre = @nombre,
                    correo = @correo,
                    telefono = @telefono
                WHERE id = @id;
                """,
                conn,
                transaction))
            {
                actualizarUsuario.Parameters.AddWithValue("@rut", rut);
                actualizarUsuario.Parameters.AddWithValue("@nombre", nombre);
                actualizarUsuario.Parameters.AddWithValue("@correo", DbValue(correo));
                actualizarUsuario.Parameters.AddWithValue("@telefono", DbValue(telefono));
                actualizarUsuario.Parameters.AddWithValue("@id", id);
                await actualizarUsuario.ExecuteNonQueryAsync(cancellationToken);
            }

            if (rol == "paciente")
            {
                await using var actualizarPaciente = new MySqlCommand(
                    """
                    UPDATE pacientes
                    SET fecha_nacimiento = @fechaNacimiento,
                        direccion = @direccion,
                        prevision = @prevision,
                        alergias = @alergias,
                        antecedentes = @antecedentes
                    WHERE usuario_id = @id;
                    """,
                    conn,
                    transaction);
                actualizarPaciente.Parameters.AddWithValue(
                    "@fechaNacimiento",
                    model.FechaNacimiento.HasValue ? model.FechaNacimiento.Value.Date : DBNull.Value);
                actualizarPaciente.Parameters.AddWithValue("@direccion", DbValue(model.Direccion));
                actualizarPaciente.Parameters.AddWithValue("@prevision", DbValue(model.Prevision));
                actualizarPaciente.Parameters.AddWithValue("@alergias", DbValue(model.Alergias));
                actualizarPaciente.Parameters.AddWithValue("@antecedentes", DbValue(model.Antecedentes));
                actualizarPaciente.Parameters.AddWithValue("@id", id);
                await actualizarPaciente.ExecuteNonQueryAsync(cancellationToken);
            }

            if (rol == "doctor")
            {
                var especialidad = model.Especialidad.Trim();
                var numeroRegistro = model.NumeroRegistro.Trim();
                if (string.IsNullOrWhiteSpace(especialidad) || string.IsNullOrWhiteSpace(numeroRegistro))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { mensaje = "La especialidad y el número de registro son obligatorios para profesionales." });
                }

                await using var actualizarDoctor = new MySqlCommand(
                    """
                    UPDATE doctores
                    SET especialidad = @especialidad,
                        numero_registro = @numeroRegistro
                    WHERE usuario_id = @id;
                    """,
                    conn,
                    transaction);
                actualizarDoctor.Parameters.AddWithValue("@especialidad", especialidad);
                actualizarDoctor.Parameters.AddWithValue("@numeroRegistro", numeroRegistro);
                actualizarDoctor.Parameters.AddWithValue("@id", id);
                await actualizarDoctor.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            var usuarioActualizado = await LeerUsuarioDetalleAsync(conn, id, cancellationToken);
            return Ok(new
            {
                mensaje = "Usuario actualizado correctamente.",
                usuario = usuarioActualizado
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { mensaje = "El RUT, correo o número de registro ya está asociado a otro usuario." });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

    [HttpPatch("citas/{id:int}/estado")]
    public async Task<IActionResult> CambiarEstadoCitaAdmin(
        int id,
        CambiarEstadoCitaRequest model,
        CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync(cancellationToken);
        if (admin.Error is not null) return admin.Error;

        var estado = model.Estado.Trim().ToLowerInvariant();
        var permitidos = new[] { "pendiente", "confirmada", "realizada", "cancelada", "no_asiste" };
        if (!permitidos.Contains(estado))
            return BadRequest(new { mensaje = "Estado de cita no válido." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(
            "UPDATE citas SET estado = @estado WHERE id = @id;",
            conn);
        cmd.Parameters.AddWithValue("@estado", estado);
        cmd.Parameters.AddWithValue("@id", id);
        var filas = await cmd.ExecuteNonQueryAsync(cancellationToken);

        return filas == 0
            ? NotFound(new { mensaje = "Cita no encontrada." })
            : Ok(new { mensaje = "Estado de la cita actualizado." });
    }

    private static async Task CrearDisponibilidadProfesionalAsync(
        MySqlConnection conn,
        MySqlTransaction transaction,
        int doctorId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO disponibilidad_doctor (doctor_id, dia_semana, hora_inicio, hora_fin, duracion_bloque_min)
            SELECT @doctorId, dias.dia, '09:00:00', '13:00:00', 30
            FROM (SELECT 1 dia UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5) dias
            UNION ALL
            SELECT @doctorId, dias.dia, '14:00:00', '18:00:00', 30
            FROM (SELECT 1 dia UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5) dias;
            """;

        await using var cmd = new MySqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("@doctorId", doctorId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<object?> LeerUsuarioDetalleAsync(
        MySqlConnection conn,
        int id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                u.id,
                u.rut,
                u.nombre,
                u.correo,
                u.telefono,
                r.nombre AS rol,
                u.activo,
                u.creado_en,
                p.fecha_nacimiento,
                p.direccion,
                p.prevision,
                p.alergias,
                p.antecedentes,
                d.especialidad,
                d.numero_registro
            FROM usuarios u
            INNER JOIN roles r ON r.id = u.rol_id
            LEFT JOIN pacientes p ON p.usuario_id = u.id
            LEFT JOIN doctores d ON d.usuario_id = u.id
            WHERE u.id = @id
            LIMIT 1;
            """;

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapUsuarioDetalle(reader) : null;
    }

    private static object MapUsuarioDetalle(MySqlDataReader reader) => new
    {
        id = reader.GetInt32("id"),
        rut = reader.GetString("rut"),
        nombre = reader.GetString("nombre"),
        correo = ReadNullableString(reader, "correo"),
        telefono = ReadNullableString(reader, "telefono"),
        rol = reader.GetString("rol"),
        activo = reader.GetBoolean("activo"),
        creadoEn = reader.GetDateTime("creado_en"),
        fechaNacimiento = reader.IsDBNull(reader.GetOrdinal("fecha_nacimiento"))
            ? (DateTime?)null
            : reader.GetDateTime("fecha_nacimiento"),
        direccion = ReadNullableString(reader, "direccion"),
        prevision = ReadNullableString(reader, "prevision"),
        alergias = ReadNullableString(reader, "alergias"),
        antecedentes = ReadNullableString(reader, "antecedentes"),
        especialidad = ReadNullableString(reader, "especialidad"),
        numeroRegistro = ReadNullableString(reader, "numero_registro")
    };

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

    private static object DbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
