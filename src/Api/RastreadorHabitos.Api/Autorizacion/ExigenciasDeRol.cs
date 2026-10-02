using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Api.Autorizacion;

public enum Exigencia
{
    Anonimo,
    Autenticado,
    Administrador
}

// Punto único del código donde se lee la exigencia de rol de cada operación [RF-CA-05].
// Los controllers no declaran autorización: esta tabla se aplica a todas sus acciones al
// arrancar, y una acción que no esté aquí impide que la API arranque.
public static class ExigenciasDeRol
{
    public const string PoliticaAutenticado = "Autenticado";
    public const string PoliticaAdministrador = "Administrador";

    private static readonly Dictionary<string, Exigencia> PorOperacion = new()
    {
        ["Cuentas.Registrar"] = Exigencia.Anonimo,
        ["Cuentas.Activar"] = Exigencia.Anonimo,
        ["Cuentas.ReenviarActivacion"] = Exigencia.Anonimo,

        ["Sesion.Iniciar"] = Exigencia.Anonimo,
        ["Sesion.UsuarioAutenticado"] = Exigencia.Autenticado,
        ["Sesion.Cerrar"] = Exigencia.Autenticado,

        ["Usuarios.Listar"] = Exigencia.Administrador,
    };

    public static Exigencia De(string controlador, string accion) =>
        PorOperacion.TryGetValue($"{controlador}.{accion}", out var exigencia)
            ? exigencia
            : throw new InvalidOperationException(
                $"La operación {controlador}.{accion} no declara su exigencia de rol en ExigenciasDeRol.");

    public static IServiceCollection AgregarExigenciasDeRol(this IServiceCollection services)
    {
        services.AddAuthorization(opciones =>
        {
            opciones.AddPolicy(PoliticaAutenticado, politica => politica.RequireAuthenticatedUser());
            opciones.AddPolicy(PoliticaAdministrador, politica => politica.RequireRole(Rol.NombreAdministrador));
        });
        return services;
    }
}

// Aplica la tabla a cada acción como metadato de su endpoint, que es lo que evalúa el
// middleware de autorización en cada petición, se construya como se construya [RD-06].
public class AplicarExigenciasDeRol : IActionModelConvention
{
    public void Apply(ActionModel accion)
    {
        object metadato = ExigenciasDeRol.De(accion.Controller.ControllerName, accion.ActionName) switch
        {
            Exigencia.Anonimo => new AllowAnonymousAttribute(),
            Exigencia.Autenticado => new AuthorizeAttribute(ExigenciasDeRol.PoliticaAutenticado),
            _ => new AuthorizeAttribute(ExigenciasDeRol.PoliticaAdministrador)
        };

        foreach (var selector in accion.Selectors)
        {
            selector.EndpointMetadata.Add(metadato);
        }
    }
}
