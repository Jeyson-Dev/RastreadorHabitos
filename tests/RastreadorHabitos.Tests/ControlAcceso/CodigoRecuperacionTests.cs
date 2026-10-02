using System.Text.RegularExpressions;
using RastreadorHabitos.Core.ControlAcceso.Reglas;

namespace RastreadorHabitos.Tests.ControlAcceso;

// [RF-CA-10] Código de un solo uso con fecha de vencimiento, enviado por correo.
public class CodigoRecuperacionTests
{
    [Fact]
    public void Generar_DevuelveDosBloquesDeCuatroSinCaracteresConfusos()
    {
        for (var i = 0; i < 500; i++)
        {
            var codigo = CodigoRecuperacion.Generar();

            Assert.Matches(new Regex("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$"), codigo);
        }
    }

    [Fact]
    public void Generar_NoRepiteCodigos()
    {
        var codigos = Enumerable.Range(0, 1000).Select(_ => CodigoRecuperacion.Generar()).ToHashSet();

        Assert.Equal(1000, codigos.Count);
    }

    [Theory]
    [InlineData("K7Q2-M9XP")]
    [InlineData("k7q2-m9xp")]
    [InlineData("K7Q2M9XP")]
    [InlineData("  k7q2 m9xp ")]
    public void Normalizar_AceptaGuionEspaciosYMinusculas(string escrito)
    {
        Assert.Equal("K7Q2M9XP", CodigoRecuperacion.Normalizar(escrito));
    }

    [Fact]
    public void Vigencia_EsDeTreintaMinutos()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), CodigoRecuperacion.Vigencia);
    }
}
