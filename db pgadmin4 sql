CREATE TABLE roles (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(20) UNIQUE NOT NULL
);

INSERT INTO roles (nombre) VALUES ('paciente'), ('doctor'), ('admin'), ('invitado');


CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    rut VARCHAR(12) UNIQUE,
    nombre VARCHAR(100) NOT NULL,
    correo VARCHAR(100) UNIQUE, 
    password TEXT,
    telefono VARCHAR(20),
    rol_id INT NOT NULL REFERENCES roles(id),
    creado_en TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE doctores (
    id SERIAL PRIMARY KEY,
    usuario_id INT UNIQUE REFERENCES usuarios(id),
    especialidad VARCHAR(100)
);


CREATE TABLE pacientes (
    id SERIAL PRIMARY KEY,
    usuario_id INT UNIQUE REFERENCES usuarios(id),
    fecha_nacimiento DATE
);


CREATE TABLE citas (
    id SERIAL PRIMARY KEY,
    paciente_id INT NOT NULL REFERENCES pacientes(id),
    doctor_id INT NOT NULL REFERENCES doctores(id),
    fecha TIMESTAMP NOT NULL,
    motivo TEXT,
    estado VARCHAR(20) DEFAULT 'pendiente'
);



INSERT INTO usuarios (rut, nombre, correo, password, telefono, rol_id)
VALUES ('12345678-9', 'Juan Perez', 'juan@mail.com', '1234', '987271476', 1);

INSERT INTO usuarios (rut, nombre, correo, password, rol_id)
VALUES ('15565678-9', 'marcelo fuentes', 'marcelo@mail.com', '4321', 2);

INSERT INTO usuarios (nombre, rol_id)
VALUES ('Invitado', 4);


SELECT * FROM usuarios;
SELECT * FROM roles;
SELECT * FROM doctores;
