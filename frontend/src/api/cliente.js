// Cliente HTTP único: agrega el token de sesión, interpreta { error } y { mensaje } de la API y
// convierte cualquier fallo en un ErrorApi con un mensaje en español que se puede mostrar tal cual.
// La autorización real la decide el servidor; aquí solo se envía la credencial.

const MENSAJES_POR_ESTADO = {
  400: 'Revisa los datos e intenta de nuevo.',
  401: 'Tu sesión terminó. Inicia sesión de nuevo.',
  403: 'No tienes permiso para hacer esto.',
  404: 'No se encontró lo que buscas.',
  409: 'La acción no se puede completar en el estado actual.',
  423: 'La cuenta está bloqueada temporalmente. Intenta más tarde.',
  503: 'El servicio no está disponible en este momento. Intenta más tarde.',
};
const MENSAJE_SERVIDOR = 'Ocurrió un error en el servidor. Intenta de nuevo en unos minutos.';
export const MENSAJE_SIN_CONEXION = 'No se pudo conectar con el servidor. Revisa tu conexión e intenta de nuevo.';

export class ErrorApi extends Error {
  constructor(mensaje, estado, datos = null) {
    super(mensaje);
    this.name = 'ErrorApi';
    this.estado = estado; // 0 cuando no hubo respuesta (sin conexión)
    this.datos = datos;
  }
}

let obtenerToken = () => null;
let alSesionInvalida = () => {};

// La sesión registra de dónde sale el token y qué hacer cuando el servidor lo rechaza (401).
export function configurarCliente({ token, sesionInvalida }) {
  if (token) obtenerToken = token;
  if (sesionInvalida) alSesionInvalida = sesionInvalida;
}

function armarUrl(ruta, consulta) {
  if (!consulta) return ruta;
  const parametros = new URLSearchParams();
  for (const [clave, valor] of Object.entries(consulta)) {
    if (valor !== undefined && valor !== null && valor !== '') parametros.set(clave, String(valor));
  }
  const texto = parametros.toString();
  return texto ? `${ruta}?${texto}` : ruta;
}

async function leerCuerpo(respuesta) {
  const tipo = respuesta.headers.get('content-type') ?? '';
  if (!tipo.includes('application/json')) return null;
  try {
    return await respuesta.json();
  } catch {
    return null;
  }
}

/**
 * Hace una petición a la API.
 * @param {string} ruta Ruta que empieza con /api.
 * @param {{ metodo?: string, cuerpo?: unknown, consulta?: Record<string, unknown>, senal?: AbortSignal, comoArchivo?: boolean }} [opciones]
 */
export async function pedir(ruta, { metodo = 'GET', cuerpo, consulta, senal, comoArchivo = false } = {}) {
  const cabeceras = { Accept: 'application/json' };
  const token = obtenerToken();
  if (token) cabeceras.Authorization = `Bearer ${token}`;

  let cuerpoEnviado;
  if (cuerpo instanceof FormData) {
    cuerpoEnviado = cuerpo; // el navegador pone el boundary del multipart
  } else if (cuerpo !== undefined) {
    cabeceras['Content-Type'] = 'application/json';
    cuerpoEnviado = JSON.stringify(cuerpo);
  }

  let respuesta;
  try {
    respuesta = await fetch(armarUrl(ruta, consulta), {
      method: metodo,
      headers: cabeceras,
      body: cuerpoEnviado,
      signal: senal,
    });
  } catch (error) {
    if (error?.name === 'AbortError') throw error;
    throw new ErrorApi(MENSAJE_SIN_CONEXION, 0);
  }

  if (respuesta.ok) {
    if (comoArchivo) return respuesta.blob();
    return leerCuerpo(respuesta);
  }

  const datos = await leerCuerpo(respuesta);
  const mensajeServidor = typeof datos?.error === 'string' && datos.error.trim() ? datos.error : null;
  const mensaje = mensajeServidor ?? MENSAJES_POR_ESTADO[respuesta.status] ?? MENSAJE_SERVIDOR;

  // Un 401 con token significa que la sesión ya no sirve (vencida, cerrada o desactivada).
  if (respuesta.status === 401 && token) alSesionInvalida(mensaje);

  // Ante un 500 nunca se muestra lo que haya venido: solo el mensaje genérico.
  throw new ErrorApi(respuesta.status >= 500 && respuesta.status !== 503 ? MENSAJE_SERVIDOR : mensaje, respuesta.status, datos);
}

// Mensaje para mostrar a partir de cualquier error capturado.
export function mensajeDeError(error) {
  if (error instanceof ErrorApi) return error.message;
  return MENSAJE_SERVIDOR;
}
