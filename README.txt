Health & Schedule
=================

Proyecto universitario para la creación de una plataforma web de agenda médica y gestión de fichas clínicas para un centro de atención en nutrición y salud mental.

Stack principal
---------------
- Frontend: Angular basado en componentes.
- Backend: ASP.NET Core Web API REST.
- Base de datos: MySQL.
- CI: GitHub Actions para build y pruebas de frontend/backend.

MVP implementado
----------------
Paciente:
- Registro con validación de RUT chileno.
- Inicio de sesión seguro.
- Reserva de horas disponibles.
- Visualización de próximas citas.
- Ficha clínica y datos personales.

Profesional:
- Agenda propia.
- Listado de pacientes asociados a citas.
- Revisión de fichas clínicas.
- Registro de atenciones.

Administrador:
- Resumen operativo del centro.
- Creación de usuarios pacientes, profesionales y administradores.
- Edición de datos de usuarios registrados.
- Activación/desactivación de cuentas.
- Gestión de citas.

Instalación local
-----------------
1. Ejecutar `hs-db/hs_db.sql` en MySQL Workbench.
2. Confirmar conexión en `hs-back-2/appsettings.json` o usando secretos locales de .NET.
3. Iniciar backend:

   cd hs-back-2
   dotnet run --launch-profile http

4. Iniciar frontend:

   cd hs-front-2
   npm ci
   npm start

5. Abrir:

   http://localhost:4200

Pruebas frontend
----------------
cd hs-front-2
npm ci
npm run test:ci
npm run build -- --configuration production

Pruebas backend
---------------
dotnet test hs-back-2.Tests/proyecto-ids-api.Tests.csproj --configuration Release

Evidencia Entrega 3
-------------------
La evidencia del cumplimiento de criterios se encuentra en:

- docs/evaluacion/ENTREGA_3_CRITERIOS_Y_EVIDENCIA.md
- docs/evaluacion/GESTION_AGIL_ENTREGA_3.md
- docs/evaluacion/BURNDOWN_ENTREGA_3.md
- docs/evaluacion/CRITERIOS_ACEPTACION.md

Buenas prácticas
----------------
- No subir `node_modules`, `dist`, `.angular`, `bin`, `obj` ni archivos temporales.
- Todo cambio importante debe entrar mediante Pull Request.
- Cada Pull Request debe tener revisión de otro integrante y GitHub Actions en verde.
- Las contraseñas se almacenan con hash PBKDF2.
- Los endpoints protegidos validan sesión y rol.
