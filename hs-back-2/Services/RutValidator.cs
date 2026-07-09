namespace proyecto_ids_api.Services;

public static class RutValidator
{
    public static string Normalize(string? rut)
    {
        if (string.IsNullOrWhiteSpace(rut)) return string.Empty;

        var limpio = new string(rut
            .Trim()
            .ToUpperInvariant()
            .Where(c => char.IsDigit(c) || c == 'K')
            .ToArray());

        if (limpio.Length < 2) return limpio;
        return $"{limpio[..^1]}-{limpio[^1]}";
    }

    public static bool IsValid(string? rut)
    {
        var normalizado = Normalize(rut);
        var partes = normalizado.Split('-');
        if (partes.Length != 2 || !int.TryParse(partes[0], out var cuerpo)) return false;
        if (partes[0].Length is < 7 or > 8) return false;

        var suma = 0;
        var multiplicador = 2;
        foreach (var caracter in partes[0].Reverse())
        {
            suma += (caracter - '0') * multiplicador;
            multiplicador = multiplicador == 7 ? 2 : multiplicador + 1;
        }

        var resultado = 11 - (suma % 11);
        var esperado = resultado switch
        {
            11 => '0',
            10 => 'K',
            _ => resultado.ToString()[0]
        };

        return partes[1][0] == esperado && cuerpo > 0;
    }
}
