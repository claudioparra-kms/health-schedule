using Microsoft.AspNetCore.Mvc;
using proyecto_ids_api.Controllers;
using Xunit;

namespace proyecto_ids_api.Tests.Controllers;

public class PublicControllerTests
{
    [Fact]
    public async Task Horarios_ShouldReturnBadRequest_WhenDoctorIdIsInvalid()
    {
        var controller = new PublicController(TestHelpers.CreateConfiguration());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Horarios(0, DateTime.Today.AddDays(1), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Horarios_ShouldReturnOkEmpty_WhenDateIsInThePast()
    {
        var controller = new PublicController(TestHelpers.CreateConfiguration());
        TestHelpers.SetAnonymousContext(controller);

        var result = await controller.Horarios(1, DateTime.Today.AddDays(-1), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Equal(Array.Empty<object>(), ok.Value);
    }
}
