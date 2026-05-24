using Npgsql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

// Prueba de conexión PostgreSQL (no bloquea el arranque si falla)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");

try
{
    using var conn = new NpgsqlConnection(connString);
    conn.Open();
    Console.WriteLine("✅ Conectado a PostgreSQL");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Sin conexión a PostgreSQL: {ex.Message}");
    Console.WriteLine("   Verifica que PostgreSQL esté corriendo y que la BD 'ihhh' exista.");
}

// Activar CORS
app.UseCors("AllowAngular");

// Controllers
app.MapControllers();

// Ejecutar app
app.Run();
