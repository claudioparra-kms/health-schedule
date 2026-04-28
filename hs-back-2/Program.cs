using Npgsql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 🔥 Controllers
builder.Services.AddControllers();

// 🔥 CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

// 🔥 Conexión PostgreSQL (opcional, solo prueba)
var connString = "Host=localhost;Port=5432;Database=ihhh;Username=postgres;Password=hola100";

using (var conn = new NpgsqlConnection(connString))
{
    conn.Open();
    Console.WriteLine("✅ Conectado a PostgreSQL");
}

// 🔥 Activar CORS
app.UseCors("AllowAngular");

// 🔥 Controllers
app.MapControllers();

// 🔥 Ejecutar app
app.Run();