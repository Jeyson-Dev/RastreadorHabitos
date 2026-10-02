using RastreadorHabitos.Core.ControlAcceso.Entities;
using RastreadorHabitos.Core.ControlAcceso.Reglas;

namespace RastreadorHabitos.Tests.ControlAcceso;

// [RF-CA-19] Tras 5 intentos fallidos consecutivos, la cuenta queda bloqueada 15 minutos.
// El sexto intento, aun con la contraseña correcta, se rechaza durante el bloqueo. Un inicio
// de sesión correcto pone el contador en cero.
public class PoliticaBloqueoTests
{
    private static readonly DateTime Ahora = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);

    private static Usuario FallarVeces(int veces)
    {
        var usuario = new Usuario();
        for (var i = 0; i < veces; i++)
        {
            PoliticaBloqueo.RegistrarFallo(usuario, Ahora);
        }
        return usuario;
    }

    [Fact]
    public void CuatroFallos_NoBloquean()
    {
        var usuario = FallarVeces(4);

        Assert.False(PoliticaBloqueo.EstaBloqueado(usuario, Ahora));
        Assert.Equal(4, usuario.IntentosFallidosConsecutivos);
    }

    [Fact]
    public void QuintoFallo_BloqueaQuinceMinutos()
    {
        var usuario = FallarVeces(5);

        Assert.True(PoliticaBloqueo.EstaBloqueado(usuario, Ahora));
        Assert.True(PoliticaBloqueo.EstaBloqueado(usuario, Ahora.AddMinutes(14).AddSeconds(59)));
        Assert.False(PoliticaBloqueo.EstaBloqueado(usuario, Ahora.AddMinutes(15)));
    }

    [Fact]
    public void InicioDeSesionCorrecto_PoneElContadorEnCero()
    {
        var usuario = FallarVeces(4);

        PoliticaBloqueo.RegistrarExito(usuario);

        Assert.Equal(0, usuario.IntentosFallidosConsecutivos);
        Assert.Null(usuario.BloqueadoHastaUtc);
    }

    [Fact]
    public void AlVencerElBloqueo_HacenFaltaOtrosCincoFallos()
    {
        var usuario = FallarVeces(5);
        var despues = Ahora.AddMinutes(16);

        for (var i = 0; i < 4; i++)
        {
            PoliticaBloqueo.RegistrarFallo(usuario, despues);
        }
        Assert.False(PoliticaBloqueo.EstaBloqueado(usuario, despues));

        PoliticaBloqueo.RegistrarFallo(usuario, despues);
        Assert.True(PoliticaBloqueo.EstaBloqueado(usuario, despues));
    }

    [Theory]
    [InlineData(0, "15 minutos")]
    [InlineData(10, "5 minutos")]
    [InlineData(14.5, "1 minuto.")]
    public void MensajeBloqueo_IndicaLosMinutosQueFaltan(double minutosTranscurridos, string esperado)
    {
        var usuario = FallarVeces(5);

        var mensaje = PoliticaBloqueo.MensajeBloqueo(usuario, Ahora.AddMinutes(minutosTranscurridos));

        Assert.Contains(esperado, mensaje);
    }
}
