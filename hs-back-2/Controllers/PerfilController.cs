using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Models;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/perfil")]
public sealed class PerfilController : ControllerBase
{
    private readonly string _connectionString;
    private readonly SessionService _sessions;
    private readonly PasswordHasher _passwordHasher;

    public PerfilController(
        IConfiguration configuration,
        SessionService sessions,
        PasswordHasher passwordHasher)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _sessions = sessions;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var sesion = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (sesion is null) return Unauthorized(new { mensaje = "La sesión expiró." });

        const string sql = """
            SELECT
                u.id,
                u.rut,
                u.nombre,
                u.correo,
                u.telefono,
                r.nombre AS rol,
                p.fecha_nacimiento,
                p.direccion,
                p.prevision,
                p.alergias,
                p.antecedentes,
                CASE
                    WHEN p.fecha_nacimiento IS NULL THEN NULL
                    ELSE TIMESTAMPDIFF(YEAR, p.fecha_nacimiento, CURDATE())
                END AS edad,
                d.especialidad,
                d.numero_registro
            FROM usuarios u
            INNER JOIN roles r ON r.id = u.rol_id
            LEFT JOIN pacientes p ON p.usuario_id = u.id
            LEFT JOIN doctores d ON d.usuario_id = u.id
            WHERE u.id = @usuarioId
            LIMIT 1;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@usuarioId", sesion.UsuarioId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return NotFound(new { mensaje = "Usuario no encontrado." });

        return Ok(new
        {
            id = reader.GetInt32("id"),
            rut = reader.GetString("rut"),
            nombre = reader.GetString("nombre"),
            correo = ReadNullableString(reader, "correo"),
            telefono = ReadNullableString(reader, "telefono"),
            rol = reader.GetString("rol"),
            fechaNacimiento = reader.IsDBNull(reader.GetOrdinal("fecha_nacimiento"))
                ? (DateTime?)null
                : reader.GetDateTime("fecha_nacimiento"),
            direccion = ReadNullableString(reader, "direccion"),
            prevision = ReadNullableString(reader, "prevision"),
            alergias = ReadNullableString(reader, "alergias"),
            antecedentes = ReadNullableString(reader, "antecedentes"),
            edad = reader.IsDBNull(reader.GetOrdinal("edad")) ? (int?)null : reader.GetInt32("edad"),
            especialidad = ReadNullableString(reader, "especialidad"),
            numeroRegistro = ReadNullableString(reader, "numero_registro")
        });
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar(
        ActualizarPerfilRequest model,
        CancellationToken cancellationToken)
    {
        var sesion = await _sessions.GetCurrentAsync(Request, cancellationToken);
        if (sesion is null) return Unauthorized(new { mensaje = "La sesión expiró." });
        if (sesion.Rol == "invitado")
            return StatusCode(403, new { mensaje = "Crea una cuenta para modificar datos personales." });

        var correo = model.Correo.Trim().ToLowerInvariant();
        var telefono = model.Telefono.Trim();
        if (string.IsNullOrWhiteSpace(correo))
            return BadRequest(new { mensaje = "El correo es obligatorio." });
        if (!string.IsNullOrWhiteSpace(model.PasswordNueva) && model.PasswordNueva.Length < 8)
            return BadRequest(new { mensaje = "La nueva contraseña debe tener al menos 8 caracteres." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            string? nuevoHash = null;
            if (!string.IsNullOrWhiteSpace(model.PasswordNueva))
            {
                if (string.IsNullOrWhiteSpace(model.PasswordActual))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { mensaje = "Ingresa tu contraseña actual para cambiarla." });
                }

                await using var obtenerHash = new MySqlCommand(
                    "SELECT password_hash FROM usuarios WHERE id = @usuarioId LIMIT 1;",
                    conn,
                    transaction);
                obtenerHash.Parameters.AddWithValue("@usuarioId", sesion.UsuarioId);
                var hashActual = Convert.ToString(await obtenerHash.ExecuteScalarAsync(cancellationToken));

                if (hashActual is null || !_passwordHasher.Verify(model.PasswordActual, hashActual))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { mensaje = "La contraseña actual no es correcta." });
                }

                nuevoHash = _passwordHasher.Hash(model.PasswordNueva);
            }

            var passwordSql = nuevoHash is null ? string.Empty : ", password_hash = @passwordHash";
            var usuarioSql = $"""
                UPDATE usuarios
                SET correo = @correo,
                    telefono = @telefono
                    {passwordSql}
                WHERE id = @usuarioId;
                """;

            await using (var actualizarUsuario = new MySqlCommand(usuarioSql, conn, transaction))
            {
                actualizarUsuario.Parameters.AddWithValue("@correo", correo);
                actualizarUsuario.Parameters.AddWithValue("@telefono", DbValue(telefono));
                actualizarUsuario.Parameters.AddWithValue("@usuarioId", sesion.UsuarioId);
                if (nuevoHash is not null) actualizarUsuario.Parameters.AddWithValue("@passwordHash", nuevoHash);
                await actualizarUsuario.ExecuteNonQueryAsync(cancellationToken);
            }

            if (sesion.PacienteId is not null)
            {
                const string pacienteSql = """
                    UPDATE pacientes
                    SET fecha_nacimiento = @fechaNacimiento,
                        direccion = @direccion,
                        prevision = @prevision,
                        alergias = @alergias,
                        antecedentes = @antecedentes
                    WHERE id = @pacienteId;
                    """;
                await using var actualizarPaciente = new MySqlCommand(pacienteSql, conn, transaction);
                actualizarPaciente.Parameters.AddWithValue(
                    "@fechaNacimiento",
                    model.FechaNacimiento.HasValue ? model.FechaNacimiento.Value.Date : DBNull.Value);
                actualizarPaciente.Parameters.AddWithValue("@direccion", DbValue(model.Direccion));
                actualizarPaciente.Parameters.AddWithValue("@prevision", DbValue(model.Prevision));
                actualizarPaciente.Parameters.AddWithValue("@alergias", DbValue(model.Alergias));
                actualizarPaciente.Parameters.AddWithValue("@antecedentes", DbValue(model.Antecedentes));
                actualizarPaciente.Parameters.AddWithValue("@pacienteId", sesion.PacienteId.Value);
                await actualizarPaciente.ExecuteNonQueryAsync(cancellationToken);
            }

            if (nuevoHash is not null)
                await _sessions.RevokeAllForUserAsync(conn, transaction, sesion.UsuarioId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Ok(new
            {
                mensaje = nuevoHash is null
                    ? "Perfil actualizado correctamente."
                    : "Perfil y contraseña actualizados. Vuelve a iniciar sesión.",
                requiereNuevoLogin = nuevoHash is not null
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { mensaje = "Ese correo ya está asociado a otra cuenta." });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string? ReadNullableString(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(column);

    private static object DbValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
