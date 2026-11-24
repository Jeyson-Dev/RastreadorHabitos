import { pedir } from './cliente';

// Control de acceso: registro, activación y contraseñas. El inicio y el cierre de sesión viven en
// sesion/ProveedorSesion.jsx, que es dueño del token.

export const registrarCuenta = ({ email, contrasena, nombreCompleto }) =>
  pedir('/api/cuentas/registro', { metodo: 'POST', cuerpo: { email, contrasena, nombreCompleto } });

export const activarCuenta = (token, senal) => pedir('/api/cuentas/activar', { consulta: { token }, senal });

export const reenviarActivacion = (email) =>
  pedir('/api/cuentas/reenviar-activacion', { metodo: 'POST', cuerpo: { email } });

export const solicitarCodigoRecuperacion = (email) =>
  pedir('/api/contrasena/recuperar', { metodo: 'POST', cuerpo: { email } });

export const restablecerContrasena = ({ email, codigo, contrasenaNueva }) =>
  pedir('/api/contrasena/restablecer', { metodo: 'POST', cuerpo: { email, codigo, contrasenaNueva } });

export const cambiarContrasena = ({ contrasenaActual, contrasenaNueva }) =>
  pedir('/api/contrasena/cambiar', { metodo: 'POST', cuerpo: { contrasenaActual, contrasenaNueva } });
