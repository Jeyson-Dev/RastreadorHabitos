import { Navigate, Outlet, useLocation } from 'react-router';
import { useSesion } from './contextoSesion';

// Pantallas de acceso: con sesión abierta no tienen sentido y se vuelve a donde se iba.
export default function RutaPublica() {
  const { autenticado } = useSesion();
  const ubicacion = useLocation();
  const desde = ubicacion.state?.desde;
  // Solo rutas internas de la app: nunca «//otro-sitio» ni direcciones absolutas.
  const destino = typeof desde === 'string' && desde.startsWith('/') && !desde.startsWith('//') ? desde : '/';
  if (autenticado) return <Navigate to={destino} replace />;
  return <Outlet />;
}
