import { pedir } from './cliente';

// Operaciones solo para el Administrador. La API rechaza a cualquier otro rol (403); el frontend
// además oculta estas pantallas.
export const CLAVES_ADMIN = {
  usuarios: ['admin', 'usuarios'],
  correos: (estado) => ['admin', 'correos', estado ?? 'todos'],
  auditoria: (filtros) => ['admin', 'auditoria', filtros],
};

export const ROLES = { Administrador: 'Administrador', Estandar: 'Estandar' };
export const ESTADOS_CORREO = ['Pendiente', 'Enviado', 'Fallido'];
export const ENTIDADES_AUDITADAS = ['Usuario', 'SolicitudPermiso', 'Documento'];

const ruta = (id, accion) => `/api/usuarios/${encodeURIComponent(id)}/${accion}`;

export const listarUsuarios = (senal) => pedir('/api/usuarios', { senal });
export const cambiarRol = (id, rol) => pedir(ruta(id, 'rol'), { metodo: 'PUT', cuerpo: { rol } });
export const desactivarUsuario = (id) => pedir(ruta(id, 'desactivar'), { metodo: 'POST' });
export const reactivarUsuario = (id) => pedir(ruta(id, 'reactivar'), { metodo: 'POST' });
export const forzarRestablecimiento = (id) => pedir(ruta(id, 'restablecer-contrasena'), { metodo: 'POST' });

export const listarCorreos = (estado, senal) => pedir('/api/correos', { consulta: { estado }, senal });

export const consultarAuditoria = ({ usuarioId, entidad, desde, hasta }, senal) =>
  pedir('/api/auditoria', { consulta: { usuarioId, entidad, desde, hasta }, senal });
