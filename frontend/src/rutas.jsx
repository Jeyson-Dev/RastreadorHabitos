import Marco from './layout/Marco';
import MarcoAcceso from './layout/MarcoAcceso';
import RutaProtegida from './sesion/RutaProtegida';
import RutaPublica from './sesion/RutaPublica';
import { ROL_ADMINISTRADOR } from './sesion/contextoSesion';
import Activar from './paginas/acceso/Activar';
import IniciarSesion from './paginas/acceso/IniciarSesion';
import Recuperar from './paginas/acceso/Recuperar';
import Registro from './paginas/acceso/Registro';
import Perfil from './paginas/Perfil';
import Habitos from './paginas/habitos/Habitos';
import Hoy from './paginas/habitos/Hoy';
import Metas from './paginas/metas/Metas';
import Notificaciones from './paginas/notificaciones/Notificaciones';
import EnConstruccion from './paginas/EnConstruccion';
import NoEncontrada from './paginas/NoEncontrada';

const pendiente = (titulo) => <EnConstruccion titulo={titulo} />;

// Árbol de rutas: las de acceso son públicas; el resto exige sesión y las de /admin, el rol.
export function crearRutas() {
  return [
    {
      element: <MarcoAcceso />,
      children: [
        {
          element: <RutaPublica />,
          children: [
            { path: '/iniciar-sesion', element: <IniciarSesion /> },
            { path: '/registro', element: <Registro /> },
            { path: '/recuperar', element: <Recuperar /> },
          ],
        },
        // La activación funciona con o sin sesión: llega desde el enlace del correo.
        { path: '/activar', element: <Activar /> },
      ],
    },
    {
      element: <RutaProtegida />,
      children: [
        {
          element: <Marco />,
          children: [
            { index: true, element: <Hoy />, handle: { titulo: 'Hoy' } },
            { path: 'habitos', element: <Habitos />, handle: { titulo: 'Hábitos' } },
            { path: 'metas', element: <Metas />, handle: { titulo: 'Metas' } },
            {
              path: 'reportes',
              // Carga diferida: Recharts solo se descarga al abrir Reportes.
              lazy: async () => ({ Component: (await import('./paginas/reportes/Reportes')).default }),
              handle: { titulo: 'Reportes' },
            },
            { path: 'notificaciones', element: <Notificaciones />, handle: { titulo: 'Notificaciones' } },
            { path: 'documentos', element: pendiente('Documentos'), handle: { titulo: 'Documentos' } },
            { path: 'perfil', element: <Perfil />, handle: { titulo: 'Mi cuenta' } },
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
