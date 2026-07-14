# Cambios para presentación universitaria

## 1. Login más profesional

Se eliminó de la pantalla de inicio de sesión el recuadro que mostraba una cuenta de demostración con RUT y contraseña. En su lugar se dejó un mensaje institucional:

- Acceso seguro para usuarios registrados.
- Credenciales entregadas por el centro.
- Reserva como invitado mediante RUT.

Esto evita que la pantalla principal parezca un prototipo de prueba durante la presentación.

## 2. Administración de usuarios

El panel de administración ahora permite editar datos de los usuarios registrados desde la sección **Usuarios**.

El administrador puede modificar:

- RUT.
- Nombre completo.
- Correo.
- Teléfono.
- Fecha de nacimiento del paciente.
- Dirección del paciente.
- Previsión.
- Alergias.
- Antecedentes relevantes.
- Especialidad del profesional.
- Número de registro del profesional.

Por seguridad, el panel administrativo no cambia contraseñas. Cada usuario puede recuperar su acceso desde **Olvidé mi contraseña**.

## 3. Backend actualizado

Se agregaron endpoints exclusivos para administradores:

```text
GET /api/admin/usuarios/{id}
PUT /api/admin/usuarios/{id}
```

Estos endpoints validan sesión de administrador, RUT chileno, duplicidad de RUT/correo/número de registro y actualizan los datos en MySQL mediante transacción.

## 4. Validación realizada

Se ejecutaron verificaciones del frontend:

```text
npm run test:ci
```

Resultado:

```text
7 archivos de prueba aprobados
29 pruebas aprobadas
0 pruebas fallidas
```

También se ejecutó:

```text
npm run build -- --configuration production
```

Resultado: compilación Angular correcta.

## 5. Nota técnica

No se ejecutó `dotnet build` en este entorno porque no estaba disponible el SDK de .NET. Los cambios de backend fueron revisados estáticamente y mantienen la conexión original con MySQL.
