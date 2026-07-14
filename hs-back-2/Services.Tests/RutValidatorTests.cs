using proyecto_ids_api.Services;
using Xunit;

namespace proyecto_ids_api.Tests.Services;

public class RutValidatorTests
{
    private static char ComputeDv(string cuerpo)
    {
        var suma = 0;
        var multiplicador = 2;
        for (int i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * multiplicador;
            multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
        }

        var resultado = 11 - (suma % 11);
        return resultado switch
        {
            11 => '0',
            10 => 'K',
            _ => resultado.ToString()[0]
        };
    }

    [Theory]
    [InlineData("11111111")]
    [InlineData("18064757")]
    [InlineData("76086428")]
    [InlineData("76133786")]
    public void IsValid_ShouldReturnTrue_ForValidRutBody(string cuerpo)
    {
        var rut = $"{cuerpo}-{ComputeDv(cuerpo)}";
        Assert.True(RutValidator.IsValid(rut));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefg")]
    [InlineData("11111111-0")]
    [InlineData("76086428-1")]
    public void IsValid_ShouldReturnFalse_ForInvalidRut(string rut)
    {
        Assert.False(RutValidator.IsValid(rut));
    }

    [Fact]
    public void Normalize_ShouldFormatRutWithDashAndUppercaseK()
    {
        var result = RutValidator.Normalize("760864285");

        Assert.Equal("76086428-5", result);
    }
}
