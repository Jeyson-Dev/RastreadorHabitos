// Guarda la credencial de sesión ({ token, expiraUtc }) entre recargas. Una credencial vencida se
// descarta al leerla; el servidor igual la rechazaría con 401.
const CLAVE = 'rh.sesion';

export function leerSesion(ahora = Date.now()) {
  try {
    const guardada = JSON.parse(localStorage.getItem(CLAVE));
    if (!guardada?.token || !guardada?.expiraUtc) return null;
    if (Date.parse(guardada.expiraUtc) <= ahora) {
      borrarSesion();
      return null;
    }
    return guardada;
  } catch {
    return null;
  }
}

export function guardarSesion({ token, expiraUtc }) {
  try {
    localStorage.setItem(CLAVE, JSON.stringify({ token, expiraUtc }));
  } catch {
    // Sin almacenamiento: la sesión dura solo mientras la pestaña siga abierta.
  }
}

export function borrarSesion() {
  try {
    localStorage.removeItem(CLAVE);
  } catch {
    // nada que borrar
  }
}
