using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using proyecto_ids_api.Services;

namespace proyecto_ids_api.Tests.Controllers;

internal static class TestHelpers
{
    public static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Port=3306;Database=hs_db;User=root;Password=test;"
        })
        .Build();

    public static void SetAnonymousContext(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public static SessionService CreateSessionService() => new SessionService(CreateConfiguration());
}
