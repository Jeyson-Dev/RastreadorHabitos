namespace RastreadorHabitos.Core.ControlAcceso.Configuracion;

// URL pública con la que se arman los enlaces de los correos. Viene de configuración y no
// de la cabecera Host de la petición, que cualquiera puede falsificar.
public class OpcionesEnlaces
{
    // Absoluta y sin barra final, por ejemplo http://localhost:5003
    public required string UrlBase { get; init; }
}
