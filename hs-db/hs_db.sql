-- ============================================================
-- Health & Schedule (H&S) - MySQL 8.0
-- Instalación local completa y reproducible
-- ============================================================

DROP DATABASE IF EXISTS hs_db;
CREATE DATABASE hs_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE hs_db;

-- ============================================================
-- SEGURIDAD Y USUARIOS
-- ============================================================

CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(20) NOT NULL UNIQUE
) ENGINE=InnoDB;

INSERT INTO roles (nombre) VALUES ('paciente'), ('doctor'), ('admin'), ('invitado');

CREATE TABLE usuarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rut VARCHAR(12) NOT NULL UNIQUE,
    nombre VARCHAR(120) NOT NULL,
    correo VARCHAR(150) NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    telefono VARCHAR(20) NULL,
    rol_id INT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_usuarios_roles FOREIGN KEY (rol_id) REFERENCES roles(id),
    CONSTRAINT chk_usuarios_rut CHECK (rut REGEXP '^[0-9]{7,8}-[0-9K]$'),
    CONSTRAINT chk_usuarios_correo CHECK (correo IS NULL OR correo REGEXP '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$')
) ENGINE=InnoDB;

CREATE TABLE sesiones (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    token_hash CHAR(64) NOT NULL UNIQUE,
    creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expira_en DATETIME NOT NULL,
    revocado_en DATETIME NULL,
    CONSTRAINT fk_sesiones_usuarios FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    INDEX idx_sesiones_usuario (usuario_id),
    INDEX idx_sesiones_expiracion (expira_en, revocado_en)
) ENGINE=InnoDB;

-- ============================================================
-- INFORMACIÓN CLÍNICA
-- ============================================================

CREATE TABLE pacientes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL UNIQUE,
    fecha_nacimiento DATE NULL,
    direccion VARCHAR(180) NULL,
    prevision VARCHAR(60) NULL,
    alergias TEXT NULL,
    antecedentes TEXT NULL,
    CONSTRAINT fk_pacientes_usuarios FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE doctores (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL UNIQUE,
    especialidad VARCHAR(100) NOT NULL,
    numero_registro VARCHAR(60) NOT NULL UNIQUE,
    CONSTRAINT fk_doctores_usuarios FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
    INDEX idx_doctores_especialidad (especialidad)
) ENGINE=InnoDB;

CREATE TABLE fichas_clinicas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT NOT NULL UNIQUE,
    observaciones_generales TEXT NULL,
    creada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizada_en DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_fichas_pacientes FOREIGN KEY (paciente_id) REFERENCES pacientes(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================================
-- AGENDA
-- ============================================================

CREATE TABLE disponibilidad_doctor (
    id INT AUTO_INCREMENT PRIMARY KEY,
    doctor_id INT NOT NULL,
    dia_semana TINYINT NOT NULL COMMENT '1=lunes, 7=domingo',
    hora_inicio TIME NOT NULL,
    hora_fin TIME NOT NULL,
    duracion_bloque_min INT NOT NULL DEFAULT 30,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_disponibilidad_doctor FOREIGN KEY (doctor_id) REFERENCES doctores(id) ON DELETE CASCADE,
    CONSTRAINT chk_disponibilidad_dia CHECK (dia_semana BETWEEN 1 AND 7),
    CONSTRAINT chk_disponibilidad_horas CHECK (hora_inicio < hora_fin),
    CONSTRAINT chk_disponibilidad_duracion CHECK (duracion_bloque_min IN (15, 20, 30, 45, 60)),
    UNIQUE KEY uq_disponibilidad (doctor_id, dia_semana, hora_inicio)
) ENGINE=InnoDB;

CREATE TABLE bloqueos_agenda (
    id INT AUTO_INCREMENT PRIMARY KEY,
    doctor_id INT NOT NULL,
    inicio DATETIME NOT NULL,
    fin DATETIME NOT NULL,
    motivo VARCHAR(180) NULL,
    CONSTRAINT fk_bloqueos_doctor FOREIGN KEY (doctor_id) REFERENCES doctores(id) ON DELETE CASCADE,
    CONSTRAINT chk_bloqueos_fechas CHECK (inicio < fin),
    INDEX idx_bloqueos_doctor_fecha (doctor_id, inicio, fin)
) ENGINE=InnoDB;

CREATE TABLE citas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT NOT NULL,
    doctor_id INT NOT NULL,
    fecha_inicio DATETIME NOT NULL,
    fecha_fin DATETIME NOT NULL,
    motivo VARCHAR(500) NULL,
    estado ENUM('pendiente', 'confirmada', 'realizada', 'cancelada', 'no_asiste') NOT NULL DEFAULT 'pendiente',
    creada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizada_en DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_citas_paciente FOREIGN KEY (paciente_id) REFERENCES pacientes(id),
    CONSTRAINT fk_citas_doctor FOREIGN KEY (doctor_id) REFERENCES doctores(id),
    CONSTRAINT chk_citas_fechas CHECK (fecha_inicio < fecha_fin),
    UNIQUE KEY uq_cita_doctor_inicio (doctor_id, fecha_inicio),
    UNIQUE KEY uq_cita_paciente_inicio (paciente_id, fecha_inicio),
    INDEX idx_citas_doctor_fecha (doctor_id, fecha_inicio),
    INDEX idx_citas_paciente_fecha (paciente_id, fecha_inicio),
    INDEX idx_citas_estado (estado)
) ENGINE=InnoDB;

CREATE TABLE atenciones (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ficha_id INT NOT NULL,
    doctor_id INT NOT NULL,
    cita_id INT NULL UNIQUE,
    fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    motivo VARCHAR(500) NOT NULL,
    diagnostico TEXT NULL,
    tratamiento TEXT NULL,
    receta TEXT NULL,
    observaciones TEXT NULL,
    CONSTRAINT fk_atenciones_ficha FOREIGN KEY (ficha_id) REFERENCES fichas_clinicas(id) ON DELETE CASCADE,
    CONSTRAINT fk_atenciones_doctor FOREIGN KEY (doctor_id) REFERENCES doctores(id),
    CONSTRAINT fk_atenciones_cita FOREIGN KEY (cita_id) REFERENCES citas(id) ON DELETE SET NULL,
    INDEX idx_atenciones_ficha_fecha (ficha_id, fecha)
) ENGINE=InnoDB;

CREATE TABLE ingresos_invitados (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    rut VARCHAR(12) NOT NULL,
    paciente_id INT NULL,
    ingresado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_invitados_paciente FOREIGN KEY (paciente_id) REFERENCES pacientes(id) ON DELETE SET NULL,
    INDEX idx_ingresos_invitados_rut (rut),
    INDEX idx_ingresos_invitados_fecha (ingresado_en)
) ENGINE=InnoDB;

-- ============================================================
-- DATOS DE DEMOSTRACIÓN
-- Las contraseñas están protegidas con PBKDF2-SHA256.
-- ============================================================

-- Credenciales:
-- Paciente: 12345678-5 / paciente123
-- Doctor Nutrición: 15565678-6 / doctor123
-- Doctor Psicología: 17654321-3 / psico123
-- Doctor Kinesiología: 14567890-0 / kine1234
-- Doctor Medicina General: 19456789-8 / medico123
-- Administrador: 11111111-1 / admin1234

INSERT INTO usuarios (rut, nombre, correo, password_hash, telefono, rol_id) VALUES
('12345678-5', 'Camila González', 'paciente@hs.local', 'pbkdf2$100000$jUQvWj/YItQfiViNY0NBzQ==$5M4b0DuQ879dV0heD9yt4uKMFp3ZXZ1KpuoKi2OTrZ0=', '+56911111111', (SELECT id FROM roles WHERE nombre='paciente')),
('9876543-3',  'Martín Rojas', 'martin@hs.local', 'pbkdf2$100000$jUQvWj/YItQfiViNY0NBzQ==$5M4b0DuQ879dV0heD9yt4uKMFp3ZXZ1KpuoKi2OTrZ0=', '+56922222222', (SELECT id FROM roles WHERE nombre='paciente')),
('15565678-6', 'Dra. Sofía Morales', 'nutricion@hs.local', 'pbkdf2$100000$X6DXmYgjgBsKb0NgswgmHA==$OkS0Rwgc9+RwYAQUTFGpOI5N1JcJRY0sr9E+DlybgiM=', '+56933333333', (SELECT id FROM roles WHERE nombre='doctor')),
('17654321-3', 'Dr. Tomás Silva', 'psicologia@hs.local', 'pbkdf2$100000$HgsaAzRalALs41ff72FnOg==$XOL+yywJ54i8PiyPtOsDz1m3mszM+IiIV91dU0AcOtM=', '+56944444444', (SELECT id FROM roles WHERE nombre='doctor')),
('14567890-0', 'Dra. Valentina Pérez', 'kinesiologia@hs.local', 'pbkdf2$100000$WnesNtBKi0IgrP6NsFx+qw==$xwxgr8EC0dSct114300RQ7ZjaizKFam3jreg1JrruY8=', '+56955555555', (SELECT id FROM roles WHERE nombre='doctor')),
('19456789-8', 'Dr. Diego Contreras', 'medicina@hs.local', 'pbkdf2$100000$Vi7uRqILiZ44T+j0Wcv8nw==$xQ9UgjxqLtEo/KbK0VVE7fbTwqTnjW7C4JV/ADt+ghM=', '+56966666666', (SELECT id FROM roles WHERE nombre='doctor')),
('11111111-1', 'Administración H&S', 'admin@hs.local', 'pbkdf2$100000$7P5+6slml+jxyQHUUJzqow==$lFggXygZtFJ6dUeiO5qVaEW6lR/mCSiOFgl1jHFHmXk=', '+56977777777', (SELECT id FROM roles WHERE nombre='admin'));

INSERT INTO pacientes (usuario_id, fecha_nacimiento, direccion, prevision, alergias, antecedentes) VALUES
((SELECT id FROM usuarios WHERE rut='12345678-5'), '1998-04-15', 'Rancagua, Región de O’Higgins', 'Fonasa', 'Alergia estacional', 'Sin antecedentes de importancia'),
((SELECT id FROM usuarios WHERE rut='9876543-3'), '1989-11-02', 'Machalí, Región de O’Higgins', 'Isapre', 'Sin alergias conocidas', 'Dolor lumbar recurrente');

INSERT INTO doctores (usuario_id, especialidad, numero_registro) VALUES
((SELECT id FROM usuarios WHERE rut='15565678-6'), 'Nutrición', 'RNPI-NU-001'),
((SELECT id FROM usuarios WHERE rut='17654321-3'), 'Psicología', 'RNPI-PS-002'),
((SELECT id FROM usuarios WHERE rut='14567890-0'), 'Kinesiología', 'RNPI-KI-003'),
((SELECT id FROM usuarios WHERE rut='19456789-8'), 'Medicina General', 'RNPI-MG-004');

INSERT INTO fichas_clinicas (paciente_id, observaciones_generales) VALUES
((SELECT p.id FROM pacientes p JOIN usuarios u ON u.id=p.usuario_id WHERE u.rut='12345678-5'), 'Paciente en control preventivo. Información de demostración.'),
((SELECT p.id FROM pacientes p JOIN usuarios u ON u.id=p.usuario_id WHERE u.rut='9876543-3'), 'Paciente en seguimiento kinésico. Información de demostración.');

-- Atención de lunes a viernes. Esto permite probar la agenda cualquier semana.
INSERT INTO disponibilidad_doctor (doctor_id, dia_semana, hora_inicio, hora_fin, duracion_bloque_min)
SELECT d.id, dias.dia, '09:00:00', '13:00:00', 30
FROM doctores d
CROSS JOIN (SELECT 1 dia UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5) dias;

INSERT INTO disponibilidad_doctor (doctor_id, dia_semana, hora_inicio, hora_fin, duracion_bloque_min)
SELECT d.id, dias.dia, '14:00:00', '18:00:00', 30
FROM doctores d
CROSS JOIN (SELECT 1 dia UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5) dias;

-- Citas dinámicas: siempre quedan cercanas y dentro de días hábiles.
SET @proximo_lunes = DATE_ADD(CURDATE(), INTERVAL (MOD(8 - DAYOFWEEK(CURDATE()), 7) + 1) DAY);
SET @proximo_martes = DATE_ADD(@proximo_lunes, INTERVAL 1 DAY);
SET @proximo_miercoles = DATE_ADD(@proximo_lunes, INTERVAL 2 DAY);
SET @lunes_historico = DATE_SUB(@proximo_lunes, INTERVAL 35 DAY);

INSERT INTO citas (paciente_id, doctor_id, fecha_inicio, fecha_fin, motivo, estado) VALUES
(
  (SELECT p.id FROM pacientes p JOIN usuarios u ON u.id=p.usuario_id WHERE u.rut='12345678-5'),
  (SELECT d.id FROM doctores d JOIN usuarios u ON u.id=d.usuario_id WHERE u.rut='15565678-6'),
  TIMESTAMP(@proximo_lunes, '10:00:00'),
  TIMESTAMP(@proximo_lunes, '10:30:00'),
  'Control nutricional', 'confirmada'
),
(
  (SELECT p.id FROM pacientes p JOIN usuarios u ON u.id=p.usuario_id WHERE u.rut='12345678-5'),
  (SELECT d.id FROM doctores d JOIN usuarios u ON u.id=d.usuario_id WHERE u.rut='17654321-3'),
  TIMESTAMP(@proximo_miercoles, '15:00:00'),
  TIMESTAMP(@proximo_miercoles, '15:30:00'),
  'Consulta de bienestar emocional', 'pendiente'
),
(
  (SELECT p.id FROM pacientes p JOIN usuarios u ON u.id=p.usuario_id WHERE u.rut='9876543-3'),
  (SELECT d.id FROM doctores d JOIN usuarios u ON u.id=d.usuario_id WHERE u.rut='14567890-0'),
  TIMESTAMP(@proximo_martes, '11:00:00'),
  TIMESTAMP(@proximo_martes, '11:30:00'),
  'Evaluación kinésica', 'confirmada'
),
(
  (SELECT p.id FROM pacientes p JOIN usuarios u ON u.id=p.usuario_id WHERE u.rut='12345678-5'),
  (SELECT d.id FROM doctores d JOIN usuarios u ON u.id=d.usuario_id WHERE u.rut='15565678-6'),
  TIMESTAMP(@lunes_historico, '09:00:00'),
  TIMESTAMP(@lunes_historico, '09:30:00'),
  'Evaluación nutricional inicial', 'realizada'
);

INSERT INTO atenciones (ficha_id, doctor_id, cita_id, fecha, motivo, diagnostico, tratamiento, receta, observaciones)
SELECT
  fc.id,
  c.doctor_id,
  c.id,
  c.fecha_inicio,
  c.motivo,
  'Estado nutricional dentro de parámetros esperados.',
  'Plan alimentario equilibrado y control en cuatro semanas.',
  'Sin indicación farmacológica.',
  'Mantener hidratación y actividad física regular.'
FROM citas c
JOIN fichas_clinicas fc ON fc.paciente_id=c.paciente_id
WHERE c.estado='realizada' AND c.motivo='Evaluación nutricional inicial'
LIMIT 1;

-- ============================================================
-- VISTAS DE APOYO
-- ============================================================

CREATE OR REPLACE VIEW vista_usuarios_roles AS
SELECT u.id, u.rut, u.nombre, u.correo, u.telefono, r.nombre AS rol, u.activo, u.creado_en
FROM usuarios u
JOIN roles r ON r.id=u.rol_id;

CREATE OR REPLACE VIEW vista_agenda_doctor AS
SELECT
    c.id AS cita_id,
    ud.nombre AS doctor,
    d.especialidad,
    up.nombre AS paciente,
    up.rut AS rut_paciente,
    c.fecha_inicio,
    c.fecha_fin,
    c.estado,
    c.motivo
FROM citas c
JOIN doctores d ON d.id=c.doctor_id
JOIN usuarios ud ON ud.id=d.usuario_id
JOIN pacientes p ON p.id=c.paciente_id
JOIN usuarios up ON up.id=p.usuario_id;

SELECT 'Base de datos H&S creada correctamente' AS resultado;
SELECT * FROM vista_usuarios_roles;
