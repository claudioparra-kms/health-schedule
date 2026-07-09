using System.Security.Cryptography;
using System.Text;
using MySqlConnector;
using proyecto_ids_api.Models;

namespace proyecto_ids_api.Services;

public sealed class SessionService
{
    private readonly string _connectionString;

    public SessionService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se configuró ConnectionStrings:DefaultConnection.");
    }

    public async Task<string> CreateAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = HashToken(token);

        const string sql = """
            INSERT INTO sesiones (usuario_id, token_hash, creado_en, expira_en)
            VALUES (@usuarioId, @tokenHash, UTC_TIMESTAMP(), DATE_ADD(UTC_TIMESTAMP(), INTERVAL 12 HOUR));
            """;

        await using var cmd = new MySqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@usuarioId", usuarioId);
        cmd.Parameters.AddWithValue("@tokenHash", tokenHash);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return token;
    }

    public async Task<SessionUser?> GetCurrentAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = ReadBearerToken(request);
        if (string.IsNullOrWhiteSpace(token)) return null;

        const string sql = """
            SELECT
                u.id,
                u.nombre,
                u.rut,
                u.correo,
                r.nombre AS rol,
                p.id AS paciente_id,
                d.id AS doctor_id,
                d.especialidad
            FROM sesiones s
            INNER JOIN usuarios u ON u.id = s.usuario_id
            INNER JOIN roles r ON r.id = u.rol_id
            LEFT JOIN pacientes p ON p.usuario_id = u.id
            LEFT JOIN doctores d ON d.usuario_id = u.id
            WHERE s.token_hash = @tokenHash
              AND s.revocado_en IS NULL
              AND s.expira_en > UTC_TIMESTAMP()
              AND u.activo = TRUE
            LIMIT 1;
            """;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tokenHash", HashToken(token));
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new SessionUser(
            reader.GetInt32("id"),
            reader.GetString("nombre"),
            reader.GetString("rut"),
            reader.IsDBNull(reader.GetOrdinal("correo")) ? null : reader.GetString("correo"),
            reader.GetString("rol"),
            reader.IsDBNull(reader.GetOrdinal("paciente_id")) ? null : reader.GetInt32("paciente_id"),
            reader.IsDBNull(reader.GetOrdinal("doctor_id")) ? null : reader.GetInt32("doctor_id"),
            reader.IsDBNull(reader.GetOrdinal("especialidad")) ? null : reader.GetString("especialidad"));
    }

    public async Task RevokeCurrentAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var token = ReadBearerToken(request);
        if (string.IsNullOrWhiteSpace(token)) return;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new MySqlCommand(
            "UPDATE sesiones SET revocado_en = UTC_TIMESTAMP() WHERE token_hash = @tokenHash;",
            conn);
        cmd.Parameters.AddWithValue("@tokenHash", HashToken(token));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = new MySqlCommand(
            "UPDATE sesiones SET revocado_en = UTC_TIMESTAMP() WHERE usuario_id = @usuarioId AND revocado_en IS NULL;",
            connection,
            transaction);
        cmd.Parameters.AddWithValue("@usuarioId", usuarioId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? header[prefix.Length..].Trim()
            : null;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
