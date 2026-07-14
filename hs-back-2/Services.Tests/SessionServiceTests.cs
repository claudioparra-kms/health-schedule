using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using proyecto_ids_api.Services;
using Xunit;

namespace proyecto_ids_api.Tests.Services;

public class SessionServiceTests
{
    [Fact]
    public void ReadBearerToken_ShouldReturnNull_WhenHeaderMissing()
    {
        var request = new DefaultHttpContext().Request;

        var result = InvokeReadBearerToken(request);

        Assert.Null(result);
    }

    [Fact]
    public void ReadBearerToken_ShouldReturnToken_WhenHeaderPresent()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Authorization = "Bearer abc123";

        var result = InvokeReadBearerToken(request);

        Assert.Equal("abc123", result);
    }

    private static string? InvokeReadBearerToken(HttpRequest request)
    {
        var type = typeof(SessionService);
        var method = type.GetMethod("ReadBearerToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return method?.Invoke(null, new object[] { request }) as string;
    }
}
