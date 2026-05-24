using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using proyecto_ids_api.Models;

namespace proyecto_ids_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada.");
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT u.id, u.nombre, u.rut, u.rol_id, r.nombre AS rol
                    FROM usuarios u
                    INNER JOIN roles r ON u.rol_id = r.id
                    WHERE u.rut = @rut AND u.password = @password
                ";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@rut", model.Rut);
                cmd.Parameters.AddWithValue("@password", model.Password);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return Unauthorized(new { mensaje = "Rut o contraseña incorrectos" });
                }

                return Ok(new
                {
                    mensaje = "Login correcto",
                    id = reader["id"],
                    nombre = reader["nombre"],
                    rut = reader["rut"],
                    rol_id = reader["rol_id"],
                    rol = reader["rol"]
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Login: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
        
        [HttpPost("Invitado")]
        public IActionResult Invitado([FromBody] InvitadoModel model)
        {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

        
            string sql = @"
                INSERT INTO usuarios (rut, nombre, rol_id)
                VALUES (@rut, 'Invitado', (SELECT id FROM roles WHERE nombre = 'invitado'))
                ON CONFLICT (rut) DO NOTHING
                ";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@rut", model.Rut);
        cmd.ExecuteNonQuery();

        return Ok(new { mensaje = "Ingreso como invitado registrado" });
        }
        catch (Exception ex)
            {
            Console.WriteLine($"Error en Invitado: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
    }
}
