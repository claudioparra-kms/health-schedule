CREATE TABLE roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(20) UNIQUE NOT NULL
);

INSERT INTO roles (nombre) VALUES 
('paciente'), 
('doctor'), 
('admin'), 
('invitado');


CREATE TABLE usuarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rut VARCHAR(12) UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(100) UNIQUE,
    password TEXT,
    telefono VARCHAR(20),
    rol_id INT NOT NULL,
    creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (rol_id) REFERENCES roles(id)
);


CREATE TABLE doctores (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT UNIQUE,
    especialidad VARCHAR(100),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);


CREATE TABLE pacientes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT UNIQUE,
    fecha_nacimiento DATE,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);


CREATE TABLE citas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paciente_id INT NOT NULL,
    doctor_id INT NOT NULL,
    fecha DATETIME NOT NULL,
    motivo TEXT,
    estado VARCHAR(20) DEFAULT 'pendiente',
    FOREIGN KEY (paciente_id) REFERENCES pacientes(id),
    FOREIGN KEY (doctor_id) REFERENCES doctores(id)
);
