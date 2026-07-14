using Microsoft.AspNetCore.Mvc;
using proyecto_ids_api.Controllers;
using proyecto_ids_api.Models;
using proyecto_ids_api.Services;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenRutIsInvalid()
    {
        var controller = CreateController();
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Login(new LoginRequest { Rut = "123", Password = "whatever" }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Invitado_ShouldReturnBadRequest_WhenRutIsInvalid()
    {
        var controller = CreateController();
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Invitado(new InvitadoRequest { Rut = "abc" }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Registro_ShouldReturnBadRequest_WhenRutIsInvalid()
    {
        var controller = CreateController();
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Registro(new RegistroRequest
        {
            Rut = "123",
            Nombre = "Test Usuario",
            Correo = "test@example.com",
            Telefono = "12345678",
            Password = "password"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Registro_ShouldReturnBadRequest_WhenPasswordTooShort()
    {
        var controller = CreateController();
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Registro(new RegistroRequest
        {
            Rut = "11111111-1",
            Nombre = "Test Usuario",
            Correo = "test@example.com",
            Telefono = "12345678",
            Password = "abc"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    private static AuthController CreateController() => new AuthController(
        TestHelpers.CreateConfiguration(),
        new PasswordHasher(),
        TestHelpers.CreateSessionService());
}
