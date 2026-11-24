import { useCallback, useEffect, useMemo, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { configurarCliente, pedir } from '../api/cliente';
import { borrarSesion, guardarSesion, leerSesion, tokenActual } from './almacenSesion';
import { CLAVE_USUARIO, ContextoSesion, ROL_ADMINISTRADOR } from './contextoSesion';

const MENSAJE_VENCIDA = 'Tu sesión venció. Inicia sesión de nuevo.';
const ESPERA_MAXIMA = 2 ** 31 - 1; // límite de setTimeout

// El cliente HTTP lee el token de forma síncrona desde el almacén: ninguna petición sale con un token
// viejo ni sin token por el orden en que React ejecuta los efectos.
configurarCliente({ token: tokenActual });

// Dueño de la sesión: consulta quién es el usuario (rol, id y permisos) y la cierra cuando el
// servidor rechaza el token vigente o cuando vence.
export default function ProveedorSesion({ children }) {
  const clienteConsultas = useQueryClient();
  const [sesion, setSesion] = useState(leerSesion);
  const [aviso, setAviso] = useState(null);

  // tokenRechazado: si llega, solo se cierra cuando es el de la sesión vigente (un 401 tardío de una
  // sesión anterior no debe cerrar la nueva).
  const terminar = useCallback(
    (mensaje, tokenRechazado) => {
      if (tokenRechazado !== undefined && tokenRechazado !== tokenActual()) return;
      borrarSesion();
      setSesion(null);
      setAviso(mensaje ?? null);
      clienteConsultas.cancelQueries();
      clienteConsultas.clear();
    },
    [clienteConsultas],
  );

  useEffect(() => {
    configurarCliente({ sesionInvalida: terminar });
  }, [terminar]);

  // La credencial dura 8 horas: al vencer se cierra aunque no haya peticiones; al volver a la
  // pestaña se revisa de nuevo, porque el temporizador se atrasa si el equipo se suspende.
  useEffect(() => {
    if (!sesion) return undefined;
    const vencio = () => Date.parse(sesion.expiraUtc) <= Date.now();
    const revisar = () => vencio() && terminar(MENSAJE_VENCIDA);
    const restante = Math.min(Math.max(Date.parse(sesion.expiraUtc) - Date.now(), 0), ESPERA_MAXIMA);
    const temporizador = setTimeout(revisar, restante);
    document.addEventListener('visibilitychange', revisar);
    return () => {
      clearTimeout(temporizador);
      document.removeEventListener('visibilitychange', revisar);
    };
  }, [sesion, terminar]);

  const consultaUsuario = useQuery({
    queryKey: CLAVE_USUARIO,
    queryFn: ({ signal }) => pedir('/api/sesion/usuario', { senal: signal }),
    enabled: Boolean(sesion),
    staleTime: 5 * 60 * 1000,
  });

  const iniciarSesion = useCallback(
    async (email, contrasena) => {
      const credencial = await pedir('/api/sesion/iniciar', { metodo: 'POST', cuerpo: { email, contrasena } });
      await clienteConsultas.cancelQueries();
      clienteConsultas.clear();
      setAviso(null);
      setSesion(guardarSesion(credencial));
    },
    [clienteConsultas],
  );

  const cerrarSesion = useCallback(async () => {
    try {
      await pedir('/api/sesion/cerrar', { metodo: 'POST' });
    } catch {
      // Aunque el servidor no responda, la sesión se olvida en este navegador.
    }
    terminar();
  }, [terminar]);

  const limpiarAviso = useCallback(() => setAviso(null), []);
  const usuario = consultaUsuario.data ?? null;
  const tienePermiso = useCallback((codigo) => Boolean(usuario?.permisos?.includes(codigo)), [usuario]);

  const valor = useMemo(
    () => ({
      autenticado: Boolean(sesion),
      usuario,
      cargandoUsuario: Boolean(sesion) && consultaUsuario.isPending,
      errorUsuario: consultaUsuario.error,
      reintentarUsuario: consultaUsuario.refetch,
      esAdministrador: usuario?.rol === ROL_ADMINISTRADOR,
      tienePermiso,
      iniciarSesion,
      cerrarSesion,
      aviso,
      limpiarAviso,
    }),
    [sesion, usuario, consultaUsuario.isPending, consultaUsuario.error, consultaUsuario.refetch, tienePermiso, iniciarSesion, cerrarSesion, aviso, limpiarAviso],
  );

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>;
}
