// Misma regla que el servidor (PoliticaContrasena): al menos 8 caracteres, con letras y números, y
// como máximo 72 bytes en UTF-8. El servidor vuelve a validar; esto solo guía mientras se escribe.
export const LONGITUD_MINIMA = 8;
export const BYTES_MAXIMOS = 72;

const tieneLetra = (texto) => /\p{L}/u.test(texto);
const tieneNumero = (texto) => /[0-9]/.test(texto);
const bytes = (texto) => new TextEncoder().encode(texto).length;

export function requisitosContrasena(contrasena = '') {
  return [
    { id: 'longitud', texto: `Al menos ${LONGITUD_MINIMA} caracteres`, cumple: contrasena.length >= LONGITUD_MINIMA },
    { id: 'letra', texto: 'Una letra', cumple: tieneLetra(contrasena) },
    { id: 'numero', texto: 'Un número', cumple: tieneNumero(contrasena) },
  ];
}

// Devuelve null si cumple; si no, el mensaje para mostrar.
export function validarContrasena(contrasena = '') {
  if (requisitosContrasena(contrasena).some((r) => !r.cumple)) {
    return 'La contraseña debe tener al menos 8 caracteres e incluir letras y números.';
  }
  if (bytes(contrasena) > BYTES_MAXIMOS) return 'La contraseña no puede superar los 72 caracteres.';
  return null;
}

// Comprobación mínima de formato antes de enviar; el servidor decide si el correo es válido.
export function pareceCorreo(texto = '') {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(texto.trim());
}
