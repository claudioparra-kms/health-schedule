using Microsoft.AspNetCore.Mvc;
using Npgsql;
using proyecto_ids_api.Models;

namespace proyecto_ids_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string connectionString =
            "Host=localhost;Port=5432;Database=ihhh;Username=postgres;Password=hola100";

        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            using var conn = new NpgsqlConnection(connectionString);
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
                return Unauthorized(new
                {
                    mensaje = "Rut o contraseña incorrectos"
                });
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
    }
}