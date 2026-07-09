using MySqlConnector;
using proyecto_ids_api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<SessionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAngular", policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("H&S");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Error no controlado al procesar {Method} {Path}", context.Request.Method, context.Request.Path);
        if (context.Response.HasStarted) throw;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            mensaje = "Ocurrió un error interno. Revisa la conexión y los registros del backend."
        });
    }
});

app.UseCors("LocalAngular");

app.MapGet("/api/salud", async (IConfiguration configuration) =>
{
    try
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        return Results.Ok(new { estado = "ok", baseDeDatos = "conectada" });
    }
    catch
    {
        return Results.Json(
            new { estado = "error", baseDeDatos = "sin conexión" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();
app.Run();
