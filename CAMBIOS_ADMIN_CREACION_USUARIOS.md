# Cambios administrativos para presentación universitaria

## Objetivo

Se profesionalizó el panel administrativo para que cumpla mejor con el rol real de un centro médico: administrar usuarios internos, pacientes y profesionales.

## Cambios implementados

### 1. Creación de usuarios desde administración

El administrador ahora puede crear cuentas nuevas directamente desde el panel:

- Profesional / Doctor
- Paciente
- Administrador

La opción se encuentra en:

```text
Panel administrativo -> Usuarios -> + Nuevo usuario
```

### 2. Alta profesional para doctores

Al crear un profesional, el sistema solicita:

- RUT
- Nombre completo
- Correo
- Teléfono
- Contraseña inicial
- Especialidad
- Número de registro profesional

Además, el backend crea automáticamente la disponibilidad base del profesional:

```text
Lunes a viernes
09:00 a 13:00
14:00 a 18:00
Bloques de 30 minutos
```

Esto permite que el profesional quede inmediatamente disponible para la agenda del centro.

### 3. Alta de pacientes

Al crear un paciente, el sistema también crea:

- Registro en usuarios
- Registro en pacientes
- Ficha clínica inicial

### 4. Seguridad

Las contraseñas iniciales se envían al backend y se almacenan como hash PBKDF2. No se guardan en texto plano en MySQL.

El panel advierte que la contraseña debe entregarse por un canal seguro y que no debe publicarse en la presentación.

### 5. Backend

Se agregó el endpoint:

```text
POST /api/admin/usuarios
```

Este endpoint valida:

- Sesión de administrador
- RUT chileno válido
- Rol permitido
- Correo obligatorio
- Contraseña mínima de 8 caracteres
- Especialidad y número de registro para profesionales
- Duplicados de RUT, correo o número de registro

### 6. Frontend

Se agregó un modal profesional con:

- Selector de tipo de cuenta
- Datos de identificación
- Contraseña inicial
- Información de paciente cuando corresponde
- Información profesional cuando corresponde
- Mensajes de error y éxito
- Actualización inmediata de la tabla y las estadísticas

## Validación realizada

Frontend Angular:

```text
npm run test:ci
7 archivos de prueba aprobados
29 pruebas aprobadas
0 pruebas fallidas
```

Build de producción:

```text
npm run build -- --configuration production
Application bundle generation complete
```

No se ejecutó `dotnet build` porque este entorno no tiene instalado el SDK de .NET. El backend fue revisado estáticamente.
