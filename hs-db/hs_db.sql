-- ============================================================
-- Health and Schedule (H&S)
-- Base de datos MySQL 8.0
-- Tema: Ficha clínica y agenda
-- Sprint: Bases del sistema de login + agenda
-- ============================================================

DROP DATABASE IF EXISTS hs_db;
CREATE DATABASE hs_db
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE hs_db;

-- ============================================================
-- 1) ROLES
-- ============================================================

CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(20) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO roles (nombre) VALUES
('paciente'),
('doctor'),
('admin'),
('invitado');

-- ============================================================
-- 2) USUARIOS
-- Guarda datos generales de login.
-- El rol define si es paciente, doctor, admin o invitado.
-- ============================================================

CREATE TABLE usuarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rut VARCHAR(12) UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(100) UNIQUE,
    password_hash TEXT,
    telefono VARCHAR(20),
    rol_id INT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_usuarios_roles
        FOREIGN KEY (rol_id) REFERENCES roles(id),

    CONSTRAINT chk_usuarios_rut
        CHECK (rut IS NULL OR rut REGEXP '^[0-9]{7,8}-[0-9Kk]$'),

    CONSTRAINT chk_usuarios_correo
        CHECK (correo IS NULL OR correo REGEXP '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'),

    CONSTRAINT chk_usuarios_telefono
        CHECK (telefono IS NULL OR telefono REGEXP '^[0-9+ -]{8,20}$')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 3) PACIENTES
-- Datos de pacientes.
-- ============================================================

CREATE TABLE pacientes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL UNIQUE,
    fecha_nacimiento DATE,
    direccion VARCHAR(150),
    prevision VARCHAR(50),
    alergias TEXT,
    antecedentes TEXT,

    CONSTRAINT fk_pacientes_usuarios
        FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 4) DOCTORES
-- Datos de doctores.
-- ============================================================

CREATE TABLE doctores (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL UNIQUE,
    especialidad VARCHAR(100) NOT NULL,
    numero_registro VARCHAR(50) UNIQUE,

    CONSTRAINT fk_doctores_usuarios
        FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 5) FICHAS CLÍNICAS
-- Una ficha por paciente.
-- ============================================================

CREATE TABLE fichas_clinicas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT NOT NULL UNIQUE,
    observaciones_generales TEXT,
    creada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizada_en DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_fichas_pacientes
        FOREIGN KEY (paciente_id) REFERENCES pacientes(id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 6) ATENCIONES MÉDICAS
-- Registros dentro de la ficha clínica.
-- ============================================================

CREATE TABLE atenciones (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ficha_id INT NOT NULL,
    doctor_id INT NOT NULL,
    fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    motivo TEXT NOT NULL,
    diagnostico TEXT,
    tratamiento TEXT,
    receta TEXT,

    CONSTRAINT fk_atenciones_fichas
        FOREIGN KEY (ficha_id) REFERENCES fichas_clinicas(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_atenciones_doctores
        FOREIGN KEY (doctor_id) REFERENCES doctores(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 7) DISPONIBILIDAD / AGENDA DEL DOCTOR
-- Define bloques disponibles de un doctor.
-- Esto permite tener agenda real, no solo citas creadas.
-- ============================================================

CREATE TABLE disponibilidad_doctor (
    id INT AUTO_INCREMENT PRIMARY KEY,
    doctor_id INT NOT NULL,
    dia_semana TINYINT NOT NULL, -- 1=lunes, 2=martes, ..., 7=domingo
    hora_inicio TIME NOT NULL,
    hora_fin TIME NOT NULL,
    duracion_bloque_min INT NOT NULL DEFAULT 30,
    activo BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT fk_disponibilidad_doctores
        FOREIGN KEY (doctor_id) REFERENCES doctores(id)
        ON DELETE CASCADE,

    CONSTRAINT chk_disponibilidad_dia
        CHECK (dia_semana BETWEEN 1 AND 7),

    CONSTRAINT chk_disponibilidad_horas
        CHECK (hora_inicio < hora_fin),

    CONSTRAINT chk_disponibilidad_duracion
        CHECK (duracion_bloque_min IN (15, 20, 30, 45, 60))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 8) BLOQUEOS DE AGENDA
-- Vacaciones, feriados, permisos o días sin atención.
-- ============================================================

CREATE TABLE bloqueos_agenda (
    id INT AUTO_INCREMENT PRIMARY KEY,
    doctor_id INT NOT NULL,
    inicio DATETIME NOT NULL,
    fin DATETIME NOT NULL,
    motivo VARCHAR(150),

    CONSTRAINT fk_bloqueos_doctores
        FOREIGN KEY (doctor_id) REFERENCES doctores(id)
        ON DELETE CASCADE,

    CONSTRAINT chk_bloqueos_fechas
        CHECK (inicio < fin)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 9) CITAS
-- Reserva concreta entre paciente y doctor.
-- La agenda se puede consultar desde esta tabla.
-- ============================================================

CREATE TABLE citas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT NOT NULL,
    doctor_id INT NOT NULL,
    fecha_inicio DATETIME NOT NULL,
    fecha_fin DATETIME NOT NULL,
    motivo TEXT,
    estado ENUM('pendiente', 'confirmada', 'realizada', 'cancelada', 'no_asiste') NOT NULL DEFAULT 'pendiente',
    creada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizada_en DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_citas_pacientes
        FOREIGN KEY (paciente_id) REFERENCES pacientes(id),

    CONSTRAINT fk_citas_doctores
        FOREIGN KEY (doctor_id) REFERENCES doctores(id),

    CONSTRAINT chk_citas_fechas
        CHECK (fecha_inicio < fecha_fin),

    -- Evita que un doctor tenga dos citas exactamente en el mismo horario de inicio.
    CONSTRAINT uq_cita_doctor_inicio
        UNIQUE (doctor_id, fecha_inicio),

    -- Evita duplicar la misma cita del paciente en el mismo horario.
    CONSTRAINT uq_cita_paciente_inicio
        UNIQUE (paciente_id, fecha_inicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE INDEX idx_citas_doctor_fecha ON citas (doctor_id, fecha_inicio);
CREATE INDEX idx_citas_paciente_fecha ON citas (paciente_id, fecha_inicio);
CREATE INDEX idx_citas_estado ON citas (estado);

-- ============================================================
-- 10) INVITADOS
-- Guarda ingresos como invitado sin crear cuenta completa.
-- ============================================================

CREATE TABLE ingresos_invitados (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rut VARCHAR(12) NOT NULL,
    ingresado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_invitados_rut
        CHECK (rut REGEXP '^[0-9]{7,8}-[0-9Kk]$')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ============================================================
-- 11) DATOS DE PRUEBA
-- IMPORTANTE: password_hash está en texto solo para pruebas académicas.
-- En producción se debe guardar hash con BCrypt.
-- ============================================================

INSERT INTO usuarios (rut, nombre, correo, password_hash, telefono, rol_id)
VALUES
('12345678-9', 'Juan Pérez', 'juan@mail.com', '1234', '987271476',
    (SELECT id FROM roles WHERE nombre = 'paciente')),
('15565678-9', 'Marcelo Fuentes', 'marcelo@mail.com', '4321', '987654321',
    (SELECT id FROM roles WHERE nombre = 'doctor')),
('11111111-1', 'Administrador H&S', 'admin@hs.cl', 'admin123', '912345678',
    (SELECT id FROM roles WHERE nombre = 'admin'));

INSERT INTO pacientes (usuario_id, fecha_nacimiento, direccion, prevision, alergias, antecedentes)
VALUES
((SELECT id FROM usuarios WHERE rut = '12345678-9'), '2000-01-01', 'Rancagua', 'Fonasa', 'Sin alergias declaradas', 'Sin antecedentes');

INSERT INTO doctores (usuario_id, especialidad, numero_registro)
VALUES
((SELECT id FROM usuarios WHERE rut = '15565678-9'), 'Medicina General', 'MED-001');

INSERT INTO fichas_clinicas (paciente_id, observaciones_generales)
VALUES
((SELECT id FROM pacientes WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '12345678-9')),
 'Ficha clínica inicial del paciente.');

INSERT INTO disponibilidad_doctor (doctor_id, dia_semana, hora_inicio, hora_fin, duracion_bloque_min)
VALUES
((SELECT id FROM doctores WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '15565678-9')), 1, '09:00:00', '13:00:00', 30),
((SELECT id FROM doctores WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '15565678-9')), 3, '14:00:00', '18:00:00', 30),
((SELECT id FROM doctores WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '15565678-9')), 5, '09:00:00', '13:00:00', 30);

INSERT INTO citas (paciente_id, doctor_id, fecha_inicio, fecha_fin, motivo, estado)
VALUES
(
    (SELECT id FROM pacientes WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '12345678-9')),
    (SELECT id FROM doctores WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '15565678-9')),
    '2026-05-04 09:00:00',
    '2026-05-04 09:30:00',
    'Chequeo general',
    'confirmada'
);

INSERT INTO atenciones (ficha_id, doctor_id, motivo, diagnostico, tratamiento, receta)
VALUES
(
    (SELECT fc.id
     FROM fichas_clinicas fc
     JOIN pacientes p ON fc.paciente_id = p.id
     JOIN usuarios u ON p.usuario_id = u.id
     WHERE u.rut = '12345678-9'),
    (SELECT id FROM doctores WHERE usuario_id = (SELECT id FROM usuarios WHERE rut = '15565678-9')),
    'Chequeo preventivo',
    'Paciente estable',
    'Control anual',
    'Sin receta'
);

-- ============================================================
-- 12) VISTAS ÚTILES
-- ============================================================

CREATE OR REPLACE VIEW vista_usuarios_roles AS
SELECT 
    u.id,
    u.rut,
    u.nombre,
    u.correo,
    u.telefono,
    r.nombre AS rol,
    u.activo,
    u.creado_en
FROM usuarios u
JOIN roles r ON u.rol_id = r.id;

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
JOIN doctores d ON c.doctor_id = d.id
JOIN usuarios ud ON d.usuario_id = ud.id
JOIN pacientes p ON c.paciente_id = p.id
JOIN usuarios up ON p.usuario_id = up.id;

-- ============================================================
-- 13) CONSULTAS DE PRUEBA
-- ============================================================

SELECT * FROM vista_usuarios_roles;
SELECT * FROM vista_agenda_doctor;
SELECT * FROM usuarios;
