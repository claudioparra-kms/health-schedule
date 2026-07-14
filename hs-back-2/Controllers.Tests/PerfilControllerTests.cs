using Microsoft.AspNetCore.Mvc;
using proyecto_ids_api.Controllers;
using proyecto_ids_api.Models;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class PerfilControllerTests
{
    [Fact]
    public async Task Obtener_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new PerfilController(
            TestHelpers.CreateConfiguration(),
            TestHelpers.CreateSessionService(),
            new proyecto_ids_api.Services.PasswordHasher());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Obtener(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Actualizar_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new PerfilController(
            TestHelpers.CreateConfiguration(),
            TestHelpers.CreateSessionService(),
            new proyecto_ids_api.Services.PasswordHasher());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Actualizar(new ActualizarPerfilRequest
        {
            Correo = "test@example.com",
            Telefono = "12345678"
        }, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }
}
