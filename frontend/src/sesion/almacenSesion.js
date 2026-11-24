// Guarda la credencial de sesión ({ token, expiraUtc }) entre recargas y mantiene en memoria la
// actual, para que el cliente HTTP la lea de forma síncrona (sin depender del orden de los efectos
// de React). Una credencial vencida o con fecha inválida se descarta; el servidor igual la rechazaría.
const CLAVE = 'rh.sesion';

let actual = null;

function esValida(credencial, ahora) {
  if (typeof credencial?.token !== 'string' || !credencial.token) return false;
  const expira = Date.parse(credencial.expiraUtc);
  return Number.isFinite(expira) && expira > ahora;
}

function leerGuardada() {
  try {
    return JSON.parse(localStorage.getItem(CLAVE));
  } catch {
    return null;
  }
}

export function leerSesion(ahora = Date.now()) {
  const guardada = leerGuardada();
  if (!esValida(guardada, ahora)) {
    borrarSesion();
    return null;
  }
  actual = { token: guardada.token, expiraUtc: guardada.expiraUtc };
  return actual;
}

export function guardarSesion(credencial) {
  if (!esValida(credencial, Date.now())) throw new Error('La credencial de sesión recibida no es válida.');
  actual = { token: credencial.token, expiraUtc: credencial.expiraUtc };
  try {
    localStorage.setItem(CLAVE, JSON.stringify(actual));
  } catch {
    // Sin almacenamiento: la sesión dura solo mientras la pestaña siga abierta.
  }
  return actual;
}

export function borrarSesion() {
  actual = null;
  try {
    localStorage.removeItem(CLAVE);
  } catch {
    // nada que borrar
  }
}

// Token de la sesión vigente, o null.
export function tokenActual() {
  return actual?.token ?? null;
}
