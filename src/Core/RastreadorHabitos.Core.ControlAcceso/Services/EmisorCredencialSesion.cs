using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public class EmisorCredencialSesion : IEmisorCredencialSesion
{
    private readonly OpcionesSesion _opcionesSesion;

    public EmisorCredencialSesion(OpcionesSesion opcionesSesion)
    {
        _opcionesSesion = opcionesSesion;
    }

    public string Emitir(SesionUsuario sesion, string nombreRol)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcionesSesion.Clave));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = OpcionesSesion.Emisor,
            Audience = OpcionesSesion.Audiencia,
            IssuedAt = sesion.FechaInicioUtc,
            NotBefore = sesion.FechaInicioUtc,
            Expires = sesion.FechaExpiracionUtc,
            Claims = new Dictionary<string, object>
            {
                [OpcionesSesion.ClaimUsuario] = sesion.UsuarioId.ToString(),
                [OpcionesSesion.ClaimSesion] = sesion.Id.ToString(),
                [OpcionesSesion.ClaimRol] = nombreRol
            },
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
