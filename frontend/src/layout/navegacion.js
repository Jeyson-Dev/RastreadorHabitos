import { Bell, ChartColumn, FileText, History, House, KeyRound, ListChecks, Mail, Target, UserRound, Users } from 'lucide-react';

// Secciones de la app. Las de administración solo se muestran al rol Administrador; el servidor
// igual rechaza a cualquier otro.
export const SECCIONES_PRINCIPALES = [
  { a: '/', etiqueta: 'Hoy', icono: House, fin: true },
  { a: '/habitos', etiqueta: 'Hábitos', icono: ListChecks },
  { a: '/metas', etiqueta: 'Metas', icono: Target },
  { a: '/reportes', etiqueta: 'Reportes', icono: ChartColumn },
  { a: '/notificaciones', etiqueta: 'Notificaciones', icono: Bell },
  { a: '/documentos', etiqueta: 'Documentos', icono: FileText },
];

export const SECCIONES_ADMINISTRACION = [
  { a: '/admin/solicitudes', etiqueta: 'Solicitudes', icono: KeyRound },
  { a: '/admin/usuarios', etiqueta: 'Usuarios', icono: Users },
  { a: '/admin/correos', etiqueta: 'Cola de correos', icono: Mail },
  { a: '/admin/auditoria', etiqueta: 'Auditoría', icono: History },
];

export const SECCION_PERFIL = { a: '/perfil', etiqueta: 'Mi cuenta', icono: UserRound };

// En móvil, la barra inferior lleva las de uso diario; el resto va en «Más».
export const RUTAS_BARRA_INFERIOR = ['/', '/habitos', '/metas', '/notificaciones'];
