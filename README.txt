Para los profesionales y administrativos de las áreas de nutrición y salud mental es importante organizar adecuadamente la información y el progreso de sus pacientes, de la misma forma que estos últimos tienen la necesidad de agendar sus atenciones de forma sencilla y rápida. Nuestro Health-Schedule es una página web con un sistema de agenda y fichas clínicas que compila todos los datos importantes de los clientes de la salud, de manera que estos puedan gestionar fácilmente sus citas y ver los resultados de estas. A diferencia de páginas web que solo permiten agendar horas de forma no eficiente, y que no incluyen información sobre la evolución de los pacientes, nuestro producto combina estas dos funciones para facilitar la labor de los profesionales y mejorar la transparencia con sus pacientes.

REQUISITOS
- .NET 10
- PostgreSQL
- Node.js
- Angular CLL21

INSTRUCCIONES
1) Clonar repositorio
2) Abrir carpeta "hs-back-2"
3) Modificar archivo appsettings.json dependiendo del usuario y contraseña Postgres
4) Crear base de datos con los comandos "psql -U postgres -c "CREATE DATABASE hs_db;" y "psql -U postgres -d hs_db -f "C:\health-schedule\hs-db\hs_db.sql" (la ruta hacia el archivo sql dependerá de donde se clone el repositorio)
5) Ejecutar los comandos "cd hs-back-2" y "dotnet run"
6) Ejecutar los comandos "cd hs-front-2", "npm install" y "ng serve"
