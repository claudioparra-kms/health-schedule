using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

var app = builder.Build();

var connString = builder.Configuration.GetConnectionString("DefaultConnection");

try
{
    using var conn = new MySqlConnection(connString);
    conn.Open();
    Console.WriteLine("✅ Conectado a MySQL");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Error conectando a MySQL: {ex.Message}");
}

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();
