using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Api.Autorizacion;

public enum Exigencia
{
    Anonimo,
    Autenticado,

    // Con sesión válida y con uno de los dos roles del sistema, verificado en cada petición
    // contra el rol vigente. Es la exigencia de las operaciones del módulo de negocio [RF-NEG-06].
    EstandarOAdministrador,

    Administrador
}

// Punto único del código donde se lee la exigencia de rol de cada operación [RF-CA-05].
// Los controllers no declaran autorización: esta tabla se aplica a todas sus acciones al
// arrancar, y una acción que no esté aquí impide que la API arranque.
public static class ExigenciasDeRol
{
    public const string PoliticaAutenticado = "Autenticado";
    public const string PoliticaEstandarOAdministrador = "EstandarOAdministrador";
    public const string PoliticaAdministrador = "Administrador";

    private static readonly Dictionary<string, Exigencia> PorOperacion = new()
    {
        ["Inicio.Estado"] = Exigencia.Anonimo,

        ["Cuentas.Registrar"] = Exigencia.Anonimo,
        ["Cuentas.Activar"] = Exigencia.Anonimo,
        ["Cuentas.ReenviarActivacion"] = Exigencia.Anonimo,

        ["Sesion.Iniciar"] = Exigencia.Anonimo,
        ["Sesion.UsuarioAutenticado"] = Exigencia.Autenticado,
        ["Sesion.Cerrar"] = Exigencia.Autenticado,

        ["Contrasena.SolicitarRecuperacion"] = Exigencia.Anonimo,
        ["Contrasena.Restablecer"] = Exigencia.Anonimo,
        ["Contrasena.Cambiar"] = Exigencia.Autenticado,

        ["Usuarios.Listar"] = Exigencia.Administrador,
        ["Usuarios.CambiarRol"] = Exigencia.Administrador,
        ["Usuarios.Desactivar"] = Exigencia.Administrador,
        ["Usuarios.Reactivar"] = Exigencia.Administrador,
        ["Usuarios.ForzarRestablecimientoContrasena"] = Exigencia.Administrador,

        ["SolicitudesPermiso.Crear"] = Exigencia.Autenticado,
        ["SolicitudesPermiso.ListarPendientes"] = Exigencia.Autenticado,
        ["SolicitudesPermiso.Aprobar"] = Exigencia.Administrador,
        ["SolicitudesPermiso.Rechazar"] = Exigencia.Administrador,

        ["Habitos.Crear"] = Exigencia.EstandarOAdministrador,
        ["Habitos.Listar"] = Exigencia.EstandarOAdministrador,
        ["Habitos.RegistrarCumplimiento"] = Exigencia.EstandarOAdministrador,
        ["Habitos.PlanDeHoy"] = Exigencia.EstandarOAdministrador,

        ["Metas.Crear"] = Exigencia.EstandarOAdministrador,
        ["Metas.Listar"] = Exigencia.EstandarOAdministrador,
        ["Metas.Obtener"] = Exigencia.EstandarOAdministrador,
        ["Metas.Completar"] = Exigencia.EstandarOAdministrador,
        ["Metas.Reactivar"] = Exigencia.EstandarOAdministrador,

        // Manejador de documentos: sesión válida y uno de los dos roles, verificado en cada
        // petición contra el rol vigente [RD-06]. Qué documentos ve cada uno lo decide la pieza.
        ["Documentos.Subir"] = Exigencia.EstandarOAdministrador,
        ["Documentos.Listar"] = Exigencia.EstandarOAdministrador,
        ["Documentos.Descargar"] = Exigencia.EstandarOAdministrador,
        ["Documentos.Eliminar"] = Exigencia.EstandarOAdministrador,
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
            opciones.AddPolicy(PoliticaEstandarOAdministrador,
                politica => politica.RequireRole(Rol.NombreEstandar, Rol.NombreAdministrador));
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
            Exigencia.EstandarOAdministrador => new AuthorizeAttribute(ExigenciasDeRol.PoliticaEstandarOAdministrador),
            _ => new AuthorizeAttribute(ExigenciasDeRol.PoliticaAdministrador)
        };

        foreach (var selector in accion.Selectors)
        {
            selector.EndpointMetadata.Add(metadato);
        }
    }
}
