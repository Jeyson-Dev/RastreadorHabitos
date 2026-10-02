namespace RastreadorHabitos.Core.Notificaciones.Configuracion;

// Se llena desde las variables de entorno Smtp__* [RF-NOT-13, RD-10]; nunca desde el repositorio.
public class OpcionesSmtp
{
    public string? Host { get; set; }
    public int? Puerto { get; set; }
    public string? Usuario { get; set; }
    public string? Contrasena { get; set; }
    public string? Remitente { get; set; }
    public bool UsarSsl { get; set; } = true;

    // Sin servidor configurado la aplicación funciona igual: los correos esperan en la cola.
    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Host) && Puerto is > 0 && !string.IsNullOrWhiteSpace(Remitente);
}
