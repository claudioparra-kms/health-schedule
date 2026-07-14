using Microsoft.AspNetCore.Mvc;
using proyecto_ids_api.Controllers;
using proyecto_ids_api.Services;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class AdminControllerTests
{
    [Fact]
    public async Task Resumen_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var controller = new AdminController(
            TestHelpers.CreateConfiguration(),
            TestHelpers.CreateSessionService(),
            new PasswordHasher());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Resumen(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }
}
