using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
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
                ?? throw new InvalidOperationException("Connection string no encontrada.");
        }

        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT u.id, u.nombre, u.rut, u.rol_id, r.nombre AS rol
                    FROM usuarios u
                    INNER JOIN roles r ON u.rol_id = r.id
                    WHERE u.rut = @rut AND u.password_hash = @password;
                ";

                using var cmd = new MySqlCommand(sql, conn);
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
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string sql = @"
                    INSERT INTO ingresos_invitados (rut)
                    VALUES (@rut);
                ";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@rut", model.Rut);
                cmd.ExecuteNonQuery();

                return Ok(new { mensaje = "Ingreso como invitado registrado", rol = "invitado" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Invitado: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        [HttpPost("Registro")]
        public IActionResult Registro([FromBody] RegistroModel model)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                using var transaction = conn.BeginTransaction();

                string sqlUsuario = @"
                    INSERT INTO usuarios (rut, nombre, correo, telefono, password_hash, rol_id)
                    VALUES (
                        @rut,
                        @nombre,
                        @correo,
                        @telefono,
                        @password,
                        (SELECT id FROM roles WHERE nombre = 'paciente')
                    );
                    SELECT LAST_INSERT_ID();
                ";

                using var cmdUsuario = new MySqlCommand(sqlUsuario, conn, transaction);
                cmdUsuario.Parameters.AddWithValue("@rut", model.Rut);
                cmdUsuario.Parameters.AddWithValue("@nombre", model.Nombre);
                cmdUsuario.Parameters.AddWithValue("@correo", model.Correo);
                cmdUsuario.Parameters.AddWithValue("@telefono", model.Telefono);
                cmdUsuario.Parameters.AddWithValue("@password", model.Password);

                int usuarioId = Convert.ToInt32(cmdUsuario.ExecuteScalar());

                string sqlPaciente = @"
                    INSERT INTO pacientes (usuario_id)
                    VALUES (@usuarioId);
                    SELECT LAST_INSERT_ID();
                ";

                using var cmdPaciente = new MySqlCommand(sqlPaciente, conn, transaction);
                cmdPaciente.Parameters.AddWithValue("@usuarioId", usuarioId);

                int pacienteId = Convert.ToInt32(cmdPaciente.ExecuteScalar());

                string sqlFicha = @"
                    INSERT INTO fichas_clinicas (paciente_id, observaciones_generales)
                    VALUES (@pacienteId, 'Ficha clínica creada automáticamente.');
                ";

                using var cmdFicha = new MySqlCommand(sqlFicha, conn, transaction);
                cmdFicha.Parameters.AddWithValue("@pacienteId", pacienteId);
                cmdFicha.ExecuteNonQuery();

                transaction.Commit();

                return Ok(new { mensaje = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Registro: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error al registrar usuario" });
            }
        }
    }
}