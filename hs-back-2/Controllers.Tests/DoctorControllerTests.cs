using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using proyecto_ids_api.Controllers;
using proyecto_ids_api.Services;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class DoctorControllerTests
{
    [Fact]
    public async Task Resumen_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.Resumen(CancellationToken.None);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);

        var payload = JsonSerializer.Serialize(unauthorized.Value);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("La sesión expiró.", document.RootElement.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Pacientes_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await controller.Pacientes(CancellationToken.None);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);

        var payload = JsonSerializer.Serialize(unauthorized.Value);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal("La sesión expiró.", document.RootElement.GetProperty("mensaje").GetString());
    }

    private static DoctorController CreateController()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Port=3306;Database=hs_db;User=root;Password=test;"
            })
            .Build();

        var sessions = new SessionService(configuration);
        return new DoctorController(configuration, sessions);
    }
}