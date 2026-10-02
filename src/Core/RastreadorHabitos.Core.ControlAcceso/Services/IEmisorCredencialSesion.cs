using RastreadorHabitos.Core.ControlAcceso.Entities;

namespace RastreadorHabitos.Core.ControlAcceso.Services;

public interface IEmisorCredencialSesion
{
    // Devuelve la credencial de sesión (JWT) que identifica esa sesión [RF-CA-03].
    string Emitir(SesionUsuario sesion, string nombreRol);
}
