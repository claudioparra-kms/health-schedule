using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Models;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly string _connectionString;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessions;

    public AuthController(
        IConfiguration configuration,
        PasswordHasher passwordHasher,
        SessionService sessions)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró la conexión a MySQL.");
        _passwordHasher = passwordHasher;
        _sessions = sessions;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest model, CancellationToken cancellationToken)
    {
        var rut = RutValidator.Normalize(model.Rut);
        if (!RutValidator.IsValid(rut))
            return BadRequest(new { mensaje = "El RUT no es válido." });

        const string sql = """
            SELECT
                u.id,
                u.nombre,
                u.rut,
                u.correo,
                u.password_hash,
                r.nombre AS rol,
                p.id AS paciente_id,
                d.id AS doctor_id,
                d.especialidad
            FROM usuarios u
            INNER JOIN roles r ON r.id = u.rol_id
            LEFT JOIN pacientes p ON p.usuario_id = u.id
            LEFT JOIN doctores d ON d.usuario_id = u.id
            WHERE u.rut = @rut AND u.activo = TRUE
            LIMIT 1;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@rut", rut);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return Unauthorized(new { mensaje = "RUT o contraseña incorrectos." });

        var passwordHash = reader.GetString("password_hash");
        if (!_passwordHasher.Verify(model.Password, passwordHash))
            return Unauthorized(new { mensaje = "RUT o contraseña incorrectos." });

        var usuario = ReadUser(reader);
        await reader.CloseAsync();

        var token = await _sessions.CreateAsync(conn, null, usuario.UsuarioId, cancellationToken);
        return Ok(CreateSessionResponse(token, usuario));
    }

    [HttpPost("invitado")]
    public async Task<IActionResult> Invitado(InvitadoRequest model, CancellationToken cancellationToken)
    {
        var rut = RutValidator.Normalize(model.Rut);
        if (!RutValidator.IsValid(rut))
            return BadRequest(new { mensaje = "El RUT no es válido." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            const string buscarSql = """
                SELECT u.id, r.nombre AS rol, p.id AS paciente_id
                FROM usuarios u
                INNER JOIN roles r ON r.id = u.rol_id
                LEFT JOIN pacientes p ON p.usuario_id = u.id
                WHERE u.rut = @rut
                LIMIT 1;
                """;

            int usuarioId;
            int pacienteId;
            string? rolExistente = null;

            await using (var buscar = new MySqlCommand(buscarSql, conn, transaction))
            {
                buscar.Parameters.AddWithValue("@rut", rut);
                await using var reader = await buscar.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    usuarioId = reader.GetInt32("id");
                    rolExistente = reader.GetString("rol");
                    pacienteId = reader.IsDBNull(reader.GetOrdinal("paciente_id"))
                        ? 0
                        : reader.GetInt32("paciente_id");
                }
                else
                {
                    usuarioId = 0;
                    pacienteId = 0;
                }
            }

            if (usuarioId > 0 && rolExistente != "invitado")
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { mensaje = "Este RUT ya tiene una cuenta. Inicia sesión con tu contraseña." });
            }

            if (usuarioId == 0)
            {
                const string insertarUsuarioSql = """
                    INSERT INTO usuarios (rut, nombre, correo, password_hash, telefono, rol_id, activo)
                    VALUES (
                        @rut,
                        'Paciente invitado',
                        NULL,
                        @passwordHash,
                        NULL,
                        (SELECT id FROM roles WHERE nombre = 'invitado'),
                        TRUE
                    );
                    SELECT LAST_INSERT_ID();
                    """;

                await using var insertarUsuario = new MySqlCommand(insertarUsuarioSql, conn, transaction);
                insertarUsuario.Parameters.AddWithValue("@rut", rut);
                insertarUsuario.Parameters.AddWithValue(
                    "@passwordHash",
                    _passwordHasher.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(24))));
                usuarioId = Convert.ToInt32(await insertarUsuario.ExecuteScalarAsync(cancellationToken));

                const string insertarPacienteSql = """
                    INSERT INTO pacientes (usuario_id) VALUES (@usuarioId);
                    SELECT LAST_INSERT_ID();
                    """;
                await using var insertarPaciente = new MySqlCommand(insertarPacienteSql, conn, transaction);
                insertarPaciente.Parameters.AddWithValue("@usuarioId", usuarioId);
                pacienteId = Convert.ToInt32(await insertarPaciente.ExecuteScalarAsync(cancellationToken));

                await using var ficha = new MySqlCommand(
                    "INSERT INTO fichas_clinicas (paciente_id, observaciones_generales) VALUES (@pacienteId, 'Ficha de invitado.');",
                    conn,
                    transaction);
                ficha.Parameters.AddWithValue("@pacienteId", pacienteId);
                await ficha.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var ingreso = new MySqlCommand(
                "INSERT INTO ingresos_invitados (rut, paciente_id) VALUES (@rut, @pacienteId);",
                conn,
                transaction))
            {
                ingreso.Parameters.AddWithValue("@rut", rut);
                ingreso.Parameters.AddWithValue("@pacienteId", pacienteId);
                await ingreso.ExecuteNonQueryAsync(cancellationToken);
            }

            var token = await _sessions.CreateAsync(conn, transaction, usuarioId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var usuario = new SessionUser(
                usuarioId,
                "Paciente invitado",
                rut,
                null,
                "invitado",
                pacienteId,
                null,
                null);

            return Ok(CreateSessionResponse(token, usuario));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registro(RegistroRequest model, CancellationToken cancellationToken)
    {
        var rut = RutValidator.Normalize(model.Rut);
        var nombre = model.Nombre.Trim();
        var correo = model.Correo.Trim().ToLowerInvariant();
        var telefono = model.Telefono.Trim();

        if (!RutValidator.IsValid(rut))
            return BadRequest(new { mensaje = "El RUT no es válido." });
        if (nombre.Length < 4)
            return BadRequest(new { mensaje = "Ingresa tu nombre completo." });
        if (model.Password.Length < 8)
            return BadRequest(new { mensaje = "La contraseña debe tener al menos 8 caracteres." });

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            int usuarioId = 0;
            int pacienteId = 0;
            string? rolExistente = null;

            const string buscarSql = """
                SELECT u.id, r.nombre AS rol, p.id AS paciente_id
                FROM usuarios u
                INNER JOIN roles r ON r.id = u.rol_id
                LEFT JOIN pacientes p ON p.usuario_id = u.id
                WHERE u.rut = @rut
                LIMIT 1;
                """;

            await using (var buscar = new MySqlCommand(buscarSql, conn, transaction))
            {
                buscar.Parameters.AddWithValue("@rut", rut);
                await using var reader = await buscar.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    usuarioId = reader.GetInt32("id");
                    rolExistente = reader.GetString("rol");
                    pacienteId = reader.IsDBNull(reader.GetOrdinal("paciente_id"))
                        ? 0
                        : reader.GetInt32("paciente_id");
                }
            }

            var hash = _passwordHasher.Hash(model.Password);

            if (usuarioId > 0 && rolExistente != "invitado")
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { mensaje = "Ya existe una cuenta con ese RUT." });
            }

            if (usuarioId > 0)
            {
                const string actualizarInvitadoSql = """
                    UPDATE usuarios
                    SET nombre = @nombre,
                        correo = @correo,
                        telefono = @telefono,
                        password_hash = @passwordHash,
                        rol_id = (SELECT id FROM roles WHERE nombre = 'paciente'),
                        activo = TRUE
                    WHERE id = @usuarioId;
                    """;
                await using var actualizar = new MySqlCommand(actualizarInvitadoSql, conn, transaction);
                actualizar.Parameters.AddWithValue("@nombre", nombre);
                actualizar.Parameters.AddWithValue("@correo", correo);
                actualizar.Parameters.AddWithValue("@telefono", DbValue(telefono));
                actualizar.Parameters.AddWithValue("@passwordHash", hash);
                actualizar.Parameters.AddWithValue("@usuarioId", usuarioId);
                await actualizar.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                const string insertarUsuarioSql = """
                    INSERT INTO usuarios (rut, nombre, correo, password_hash, telefono, rol_id, activo)
                    VALUES (
                        @rut,
                        @nombre,
                        @correo,
                        @passwordHash,
                        @telefono,
                        (SELECT id FROM roles WHERE nombre = 'paciente'),
                        TRUE
                    );
                    SELECT LAST_INSERT_ID();
                    """;
                await using var insertarUsuario = new MySqlCommand(insertarUsuarioSql, conn, transaction);
                insertarUsuario.Parameters.AddWithValue("@rut", rut);
                insertarUsuario.Parameters.AddWithValue("@nombre", nombre);
                insertarUsuario.Parameters.AddWithValue("@correo", correo);
                insertarUsuario.Parameters.AddWithValue("@passwordHash", hash);
                insertarUsuario.Parameters.AddWithValue("@telefono", DbValue(telefono));
                usuarioId = Convert.ToInt32(await insertarUsuario.ExecuteScalarAsync(cancellationToken));

                await using var insertarPaciente = new MySqlCommand(
                    "INSERT INTO pacientes (usuario_id) VALUES (@usuarioId); SELECT LAST_INSERT_ID();",
                    conn,
                    transaction);
                insertarPaciente.Parameters.AddWithValue("@usuarioId", usuarioId);
                pacienteId = Convert.ToInt32(await insertarPaciente.ExecuteScalarAsync(cancellationToken));

                await using var insertarFicha = new MySqlCommand(
                    "INSERT INTO fichas_clinicas (paciente_id, observaciones_generales) VALUES (@pacienteId, 'Ficha clínica creada al registrar la cuenta.');",
                    conn,
                    transaction);
                insertarFicha.Parameters.AddWithValue("@pacienteId", pacienteId);
                await insertarFicha.ExecuteNonQueryAsync(cancellationToken);
            }

            if (rolExistente == "invitado")
                await _sessions.RevokeAllForUserAsync(conn, transaction, usuarioId, cancellationToken);

            var token = await _sessions.CreateAsync(conn, transaction, usuarioId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var usuario = new SessionUser(
                usuarioId,
                nombre,
                rut,
                correo,
                "paciente",
                pacienteId,
                null,
                null);

            return Ok(CreateSessionResponse(token, usuario));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { mensaje = "El RUT o correo ya está registrado." });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    [HttpPost("recuperar-password")]
    public async Task<IActionResult> RecuperarPassword(
        RecuperarPasswordRequest model,
        CancellationToken cancellationToken)
    {
        var valor = model.RutOCorreo.Trim();
        if (string.IsNullOrWhiteSpace(valor))
            return BadRequest(new { mensaje = "Ingresa tu RUT o correo." });

        if (!valor.Contains('@')) valor = RutValidator.Normalize(valor);

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);

        const string buscarSql = "SELECT id FROM usuarios WHERE (rut = @valor OR correo = @valor) AND activo = TRUE LIMIT 1;";
        await using var buscar = new MySqlCommand(buscarSql, conn, transaction);
        buscar.Parameters.AddWithValue("@valor", valor.ToLowerInvariant());
        var usuarioIdObj = await buscar.ExecuteScalarAsync(cancellationToken);

        if (usuarioIdObj is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return NotFound(new { mensaje = "No encontramos una cuenta activa con esos datos." });
        }

        var usuarioId = Convert.ToInt32(usuarioIdObj);
        var temporal = GenerateTemporaryPassword(10);

        await using var actualizar = new MySqlCommand(
            "UPDATE usuarios SET password_hash = @hash WHERE id = @usuarioId;",
            conn,
            transaction);
        actualizar.Parameters.AddWithValue("@hash", _passwordHasher.Hash(temporal));
        actualizar.Parameters.AddWithValue("@usuarioId", usuarioId);
        await actualizar.ExecuteNonQueryAsync(cancellationToken);
        await _sessions.RevokeAllForUserAsync(conn, transaction, usuarioId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            mensaje = "Contraseña temporal generada. En este prototipo local se muestra en pantalla.",
            passwordTemporal = temporal,
            modoLocal = true
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var usuario = await _sessions.GetCurrentAsync(Request, cancellationToken);
        return usuario is null
            ? Unauthorized(new { mensaje = "La sesión expiró. Inicia sesión nuevamente." })
            : Ok(new { usuario = ToUserObject(usuario) });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _sessions.RevokeCurrentAsync(Request, cancellationToken);
        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    private static SessionUser ReadUser(MySqlDataReader reader) => new(
        reader.GetInt32("id"),
        reader.GetString("nombre"),
        reader.GetString("rut"),
        reader.IsDBNull(reader.GetOrdinal("correo")) ? null : reader.GetString("correo"),
        reader.GetString("rol"),
        reader.IsDBNull(reader.GetOrdinal("paciente_id")) ? null : reader.GetInt32("paciente_id"),
        reader.IsDBNull(reader.GetOrdinal("doctor_id")) ? null : reader.GetInt32("doctor_id"),
        reader.IsDBNull(reader.GetOrdinal("especialidad")) ? null : reader.GetString("especialidad"));

    private static object CreateSessionResponse(string token, SessionUser usuario) => new
    {
        token,
        usuario = ToUserObject(usuario)
    };

    private static object ToUserObject(SessionUser usuario) => new
    {
        id = usuario.UsuarioId,
        nombre = usuario.Nombre,
        rut = usuario.Rut,
        correo = usuario.Correo,
        rol = usuario.Rol,
        pacienteId = usuario.PacienteId,
        doctorId = usuario.DoctorId,
        especialidad = usuario.Especialidad
    };

    private static object DbValue(string value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    private static string GenerateTemporaryPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
