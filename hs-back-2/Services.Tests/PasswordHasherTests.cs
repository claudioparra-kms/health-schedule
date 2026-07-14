using System.Text.RegularExpressions;
using proyecto_ids_api.Services;
using Xunit;

namespace proyecto_ids_api.Tests.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnNonEmptyHash()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.Hash("password123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Contains("pbkdf2$", hash);
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForCorrectPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("password123");

        Assert.True(hasher.Verify("password123", hash));
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForWrongPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("password123");

        Assert.False(hasher.Verify("wrongpass", hash));
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForMalformedHash()
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("password123", "bad-hash-format"));
    }
}
