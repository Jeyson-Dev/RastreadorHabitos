import { Navigate, Outlet, useLocation } from 'react-router';
import { useSesion } from './contextoSesion';

// Pantallas de acceso: con sesión abierta no tienen sentido y se vuelve a donde se iba.
export default function RutaPublica() {
  const { autenticado } = useSesion();
  const ubicacion = useLocation();
  if (autenticado) return <Navigate to={ubicacion.state?.desde ?? '/'} replace />;
  return <Outlet />;
}
