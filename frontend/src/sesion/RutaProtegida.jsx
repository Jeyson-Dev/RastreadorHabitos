import { Navigate, Outlet, useLocation } from 'react-router';
import Cargando from '../componentes/Cargando';
import EstadoError from '../componentes/EstadoError';
import SinPermiso from '../paginas/SinPermiso';
import { useSesion } from './contextoSesion';

// Exige sesión y, si se indica, un rol. Ocultar la ruta es solo comodidad: el servidor decide.
export default function RutaProtegida({ rol }) {
  const { autenticado, usuario, cargandoUsuario, errorUsuario, reintentarUsuario } = useSesion();
  const ubicacion = useLocation();

  if (!autenticado) {
    return <Navigate to="/iniciar-sesion" replace state={{ desde: ubicacion.pathname + ubicacion.search }} />;
  }
  if (cargandoUsuario) return <Cargando pantalla texto="Cargando tu cuenta…" />;
  if (!usuario) {
    return (
      <div className="mx-auto max-w-md p-6">
        <EstadoError error={errorUsuario} alReintentar={reintentarUsuario} titulo="No se pudo cargar tu cuenta" />
      </div>
    );
  }
  if (rol && usuario.rol !== rol) return <SinPermiso />;
  return <Outlet />;
}
