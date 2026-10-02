using RastreadorHabitos.Core.ControlAcceso.Reglas;

namespace RastreadorHabitos.Tests.ControlAcceso;

// [RF-CA-14] Al menos 8 caracteres, con letras y números; rechazo con mensaje controlado [RD-07].
public class PoliticaContrasenaTests
{
    [Theory]
    [InlineData("abc12")]          // 5 caracteres: el caso que prueba el profesor
    [InlineData("abcdefgh")]       // sin números
    [InlineData("12345678")]       // sin letras
    [InlineData("")]
    [InlineData(null)]
    public void Validar_RechazaContrasenaQueNoCumpleLaPolitica(string? contrasena)
    {
        Assert.Equal(PoliticaContrasena.MensajeIncumplimiento, PoliticaContrasena.Validar(contrasena));
    }

    [Fact]
    public void Validar_RechazaDigitosQueNoSonDel0Al9()
    {
        // "١٢٣" son dígitos arábigo-índicos: char.IsDigit los acepta, la política no.
        Assert.Equal(PoliticaContrasena.MensajeIncumplimiento, PoliticaContrasena.Validar("abcdefg١٢٣"));
    }

    [Theory]
    [InlineData("abcdefg1")]       // exactamente 8
    [InlineData("contraseña2026")] // letras con ñ
    public void Validar_AceptaContrasenaQueCumpleLaPolitica(string contrasena)
    {
        Assert.Null(PoliticaContrasena.Validar(contrasena));
    }

    [Fact]
    public void Validar_AceptaHasta72BytesYRechazaMas()
    {
        var limite = new string('a', 71) + "1";
        Assert.Null(PoliticaContrasena.Validar(limite));
        Assert.Equal(PoliticaContrasena.MensajeDemasiadoLarga, PoliticaContrasena.Validar(limite + "x"));
    }
}
