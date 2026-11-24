import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, RouterProvider } from 'react-router';
import '@fontsource-variable/inter';
import './estilos.css';
import ProveedorTema from './tema/ProveedorTema';
import ProveedorSesion from './sesion/ProveedorSesion';
import { crearRutas } from './rutas';

// Solo se reintenta lo que puede ser pasajero (sin conexión o 5xx); un 4xx no cambia al repetirlo.
const clienteConsultas = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30 * 1000,
      retry: (intentos, error) => (error?.estado === 0 || error?.estado >= 500) && intentos < 2,
    },
  },
});

const enrutador = createBrowserRouter(crearRutas());

createRoot(document.getElementById('raiz')).render(
  <StrictMode>
    <QueryClientProvider client={clienteConsultas}>
      <ProveedorTema>
        <ProveedorSesion>
          <RouterProvider router={enrutador} />
        </ProveedorSesion>
      </ProveedorTema>
    </QueryClientProvider>
  </StrictMode>,
);
