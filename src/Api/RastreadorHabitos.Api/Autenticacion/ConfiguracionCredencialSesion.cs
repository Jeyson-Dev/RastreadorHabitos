using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RastreadorHabitos.Core.ControlAcceso.Configuracion;
using RastreadorHabitos.Core.ControlAcceso.Services;

namespace RastreadorHabitos.Api.Autenticacion;

// Punto único de la validación de la credencial de sesión en cada petición [RF-CA-03, RF-CA-07].
public static class ConfiguracionCredencialSesion
{
    public static IServiceCollection AgregarCredencialSesion(this IServiceCollection services, OpcionesSesion opciones)
    {
        services.AddSingleton(opciones);
        services.AddSingleton<IEmisorCredencialSesion, EmisorCredencialSesion>();
        services.AddScoped<ISesionService, SesionService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                // Los claims se leen con su nombre original (sub, sid, role).
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = OpcionesSesion.Emisor,
                    ValidAudience = OpcionesSesion.Audiencia,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Clave)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = OpcionesSesion.ClaimUsuario,
                    RoleClaimType = OpcionesSesion.ClaimRol
                };

                jwt.Events = new JwtBearerEvents
                {
                    // Una firma válida no basta: la sesión debe seguir abierta. Así el cierre de
                    // sesión invalida la credencial de verdad [RF-CA-18].
                    OnTokenValidated = async context =>
                    {
                        var sesionClaim = context.Principal?.FindFirst(OpcionesSesion.ClaimSesion)?.Value;
                        var sesiones = context.HttpContext.RequestServices.GetRequiredService<ISesionService>();

                        if (!Guid.TryParse(sesionClaim, out var sesionId) || !await sesiones.EstaAbiertaAsync(sesionId))
                        {
                            context.Fail("La sesión no está abierta.");
                        }
                    },

                    // Cualquier rechazo responde lo mismo, sin revelar el motivo [RD-08].
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { error = "Se requiere una sesión válida." });
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
