import Marco from './layout/Marco';
import RutaProtegida from './sesion/RutaProtegida';
import RutaPublica from './sesion/RutaPublica';
import { ROL_ADMINISTRADOR } from './sesion/contextoSesion';
import IniciarSesion from './paginas/acceso/IniciarSesion';
import EnConstruccion from './paginas/EnConstruccion';
import NoEncontrada from './paginas/NoEncontrada';

const pendiente = (titulo) => <EnConstruccion titulo={titulo} />;

// Árbol de rutas: las de acceso son públicas; el resto exige sesión y las de /admin, el rol.
export function crearRutas() {
  return [
    {
      element: <RutaPublica />,
      children: [
        { path: '/iniciar-sesion', element: <IniciarSesion /> },
        { path: '/registro', element: <main className="p-6">{pendiente('Crear cuenta')}</main> },
        { path: '/recuperar', element: <main className="p-6">{pendiente('Recuperar contraseña')}</main> },
      ],
    },
    // La activación funciona con o sin sesión: llega desde el enlace del correo.
    { path: '/activar', element: <main className="p-6">{pendiente('Activar cuenta')}</main> },
    {
      element: <RutaProtegida />,
      children: [
        {
          element: <Marco />,
          children: [
            { index: true, element: pendiente('Hoy'), handle: { titulo: 'Hoy' } },
            { path: 'habitos', element: pendiente('Hábitos'), handle: { titulo: 'Hábitos' } },
            { path: 'metas', element: pendiente('Metas'), handle: { titulo: 'Metas' } },
            { path: 'reportes', element: pendiente('Reportes'), handle: { titulo: 'Reportes' } },
            { path: 'notificaciones', element: pendiente('Notificaciones'), handle: { titulo: 'Notificaciones' } },
            { path: 'documentos', element: pendiente('Documentos'), handle: { titulo: 'Documentos' } },
            { path: 'perfil', element: pendiente('Mi cuenta'), handle: { titulo: 'Mi cuenta' } },
            {
              path: 'admin',
              element: <RutaProtegida rol={ROL_ADMINISTRADOR} />,
              children: [
                { path: 'solicitudes', element: pendiente('Solicitudes de permiso'), handle: { titulo: 'Solicitudes de permiso' } },
                { path: 'usuarios', element: pendiente('Usuarios y roles'), handle: { titulo: 'Usuarios y roles' } },
                { path: 'correos', element: pendiente('Cola de correos'), handle: { titulo: 'Cola de correos' } },
                { path: 'auditoria', element: pendiente('Auditoría'), handle: { titulo: 'Auditoría' } },
              ],
            },
            { path: '*', element: <NoEncontrada />, handle: { titulo: 'No encontrada' } },
          ],
        },
      ],
    },
  ];
}
