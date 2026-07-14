using Microsoft.AspNetCore.Mvc;
using proyecto_ids_api.Controllers;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class FichasControllerTests
{
    [Fact]
    public async Task MiFicha_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new FichasController(TestHelpers.CreateConfiguration(), TestHelpers.CreateSessionService());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.MiFicha(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task FichaPaciente_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new FichasController(TestHelpers.CreateConfiguration(), TestHelpers.CreateSessionService());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.FichaPaciente(1, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }
}
