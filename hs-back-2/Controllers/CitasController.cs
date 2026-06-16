using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using proyecto_ids_api.Models;

namespace proyecto_ids_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CitasController : ControllerBase
    {
        private readonly string _connectionString;

        public CitasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string no encontrada.");
        }

        [HttpPost("Crear")]
        public IActionResult Crear([FromBody] CrearCitaModel model)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                if (model.FechaInicio >= model.FechaFin)
                {
                    return BadRequest(new { mensaje = "La fecha de inicio debe ser menor que la fecha de fin." });
                }

                string sqlExistePaciente = "SELECT COUNT(*) FROM pacientes WHERE id = @pacienteId;";
                using var cmdPaciente = new MySqlCommand(sqlExistePaciente, conn);
                cmdPaciente.Parameters.AddWithValue("@pacienteId", model.PacienteId);

                if (Convert.ToInt32(cmdPaciente.ExecuteScalar()) == 0)
                {
                    return BadRequest(new { mensaje = "El paciente no existe." });
                }

                string sqlExisteDoctor = "SELECT COUNT(*) FROM doctores WHERE id = @doctorId;";
                using var cmdDoctor = new MySqlCommand(sqlExisteDoctor, conn);
                cmdDoctor.Parameters.AddWithValue("@doctorId", model.DoctorId);

                if (Convert.ToInt32(cmdDoctor.ExecuteScalar()) == 0)
                {
                    return BadRequest(new { mensaje = "El doctor no existe." });
                }

                string sqlChoque = @"
                    SELECT COUNT(*)
                    FROM citas
                    WHERE doctor_id = @doctorId
                    AND estado IN ('pendiente', 'confirmada')
                    AND (@fechaInicio < fecha_fin AND @fechaFin > fecha_inicio);
                ";

                using var cmdChoque = new MySqlCommand(sqlChoque, conn);
                cmdChoque.Parameters.AddWithValue("@doctorId", model.DoctorId);
                cmdChoque.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
                cmdChoque.Parameters.AddWithValue("@fechaFin", model.FechaFin);

                int choques = Convert.ToInt32(cmdChoque.ExecuteScalar());

                if (choques > 0)
                {
                    return BadRequest(new { mensaje = "El doctor ya tiene una cita en ese horario." });
                }

                string sqlBloqueo = @"
                    SELECT COUNT(*)
                    FROM bloqueos_agenda
                    WHERE doctor_id = @doctorId
                    AND (@fechaInicio < fin AND @fechaFin > inicio);
                ";

                using var cmdBloqueo = new MySqlCommand(sqlBloqueo, conn);
                cmdBloqueo.Parameters.AddWithValue("@doctorId", model.DoctorId);
                cmdBloqueo.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
                cmdBloqueo.Parameters.AddWithValue("@fechaFin", model.FechaFin);

                int bloqueos = Convert.ToInt32(cmdBloqueo.ExecuteScalar());

                if (bloqueos > 0)
                {
                    return BadRequest(new { mensaje = "El doctor tiene la agenda bloqueada en ese horario." });
                }

                string sqlInsert = @"
                    INSERT INTO citas
                    (paciente_id, doctor_id, fecha_inicio, fecha_fin, motivo, estado)
                    VALUES
                    (@pacienteId, @doctorId, @fechaInicio, @fechaFin, @motivo, 'pendiente');
                ";

                using var cmdInsert = new MySqlCommand(sqlInsert, conn);
                cmdInsert.Parameters.AddWithValue("@pacienteId", model.PacienteId);
                cmdInsert.Parameters.AddWithValue("@doctorId", model.DoctorId);
                cmdInsert.Parameters.AddWithValue("@fechaInicio", model.FechaInicio);
                cmdInsert.Parameters.AddWithValue("@fechaFin", model.FechaFin);
                cmdInsert.Parameters.AddWithValue("@motivo", model.Motivo);

                cmdInsert.ExecuteNonQuery();

                return Ok(new { mensaje = "Cita creada correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando cita: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno al crear cita" });
            }
        }

        [HttpGet("Doctor/{doctorId}")]
        public IActionResult AgendaDoctor(int doctorId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT 
                        c.id,
                        c.fecha_inicio,
                        c.fecha_fin,
                        c.estado,
                        c.motivo,
                        u.nombre AS paciente
                    FROM citas c
                    INNER JOIN pacientes p ON c.paciente_id = p.id
                    INNER JOIN usuarios u ON p.usuario_id = u.id
                    WHERE c.doctor_id = @doctorId
                    ORDER BY c.fecha_inicio;
                ";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@doctorId", doctorId);

                using var reader = cmd.ExecuteReader();

                var citas = new List<object>();

                while (reader.Read())
                {
                    citas.Add(new
                    {
                        id = reader["id"],
                        fechaInicio = reader["fecha_inicio"],
                        fechaFin = reader["fecha_fin"],
                        estado = reader["estado"],
                        motivo = reader["motivo"],
                        paciente = reader["paciente"]
                    });
                }

                return Ok(citas);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error agenda doctor: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpGet("Paciente/{pacienteId}")]
        public IActionResult CitasPaciente(int pacienteId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT 
                        c.id,
                        c.fecha_inicio,
                        c.fecha_fin,
                        c.estado,
                        c.motivo,
                        u.nombre AS doctor
                    FROM citas c
                    INNER JOIN doctores d ON c.doctor_id = d.id
                    INNER JOIN usuarios u ON d.usuario_id = u.id
                    WHERE c.paciente_id = @pacienteId
                    ORDER BY c.fecha_inicio;
                ";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pacienteId", pacienteId);

                using var reader = cmd.ExecuteReader();

                var citas = new List<object>();

                while (reader.Read())
                {
                    citas.Add(new
                    {
                        id = reader["id"],
                        fechaInicio = reader["fecha_inicio"],
                        fechaFin = reader["fecha_fin"],
                        estado = reader["estado"],
                        motivo = reader["motivo"],
                        doctor = reader["doctor"]
                    });
                }

                return Ok(citas);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error citas paciente: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpPut("Cancelar/{id}")]
        public IActionResult Cancelar(int id)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();

                string sql = @"
                    UPDATE citas
                    SET estado = 'cancelada'
                    WHERE id = @id;
                ";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int filas = cmd.ExecuteNonQuery();

                if (filas == 0)
                {
                    return NotFound(new { mensaje = "Cita no encontrada" });
                }

                return Ok(new { mensaje = "Cita cancelada correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelando cita: {ex.Message}");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }
    }
}