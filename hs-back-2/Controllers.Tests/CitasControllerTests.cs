using Microsoft.AspNetCore.Mvc;
using proyecto_ids_api.Controllers;
using proyecto_ids_api.Models;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class CitasControllerTests
{
    [Fact]
    public async Task Crear_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new CitasController(TestHelpers.CreateConfiguration(), TestHelpers.CreateSessionService());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Crear(new CrearCitaRequest
        {
            DoctorId = 1,
            FechaInicio = DateTime.Now.AddDays(1)
        }, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Mias_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new CitasController(TestHelpers.CreateConfiguration(), TestHelpers.CreateSessionService());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Mias(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }
}
