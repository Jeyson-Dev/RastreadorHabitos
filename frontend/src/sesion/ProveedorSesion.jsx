import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { configurarCliente, pedir } from '../api/cliente';
import { borrarSesion, guardarSesion, leerSesion } from './almacenSesion';
import { CLAVE_USUARIO, ContextoSesion, ROL_ADMINISTRADOR } from './contextoSesion';

const MENSAJE_VENCIDA = 'Tu sesión venció. Inicia sesión de nuevo.';

// Dueño de la sesión: guarda el token, lo entrega al cliente HTTP, consulta quién es el usuario
// (rol, id y permisos) y la cierra cuando el servidor la rechaza o cuando vence.
export default function ProveedorSesion({ children }) {
  const clienteConsultas = useQueryClient();
  const [sesion, setSesion] = useState(leerSesion);
  const [aviso, setAviso] = useState(null);
  const tokenActual = useRef(sesion?.token ?? null);

  // Se sincroniza antes que la consulta del usuario (los efectos corren en orden de declaración).
  useEffect(() => {
    tokenActual.current = sesion?.token ?? null;
  }, [sesion]);

  const terminar = useCallback(
    (mensaje) => {
      borrarSesion();
      setSesion(null);
      setAviso(mensaje ?? null);
      clienteConsultas.clear();
    },
    [clienteConsultas],
  );

  useEffect(() => {
    configurarCliente({ token: () => tokenActual.current, sesionInvalida: (mensaje) => terminar(mensaje) });
  }, [terminar]);

  // La credencial dura 8 horas: al vencer, se cierra aunque no haya peticiones en curso.
  useEffect(() => {
    if (!sesion) return undefined;
    const restante = Date.parse(sesion.expiraUtc) - Date.now();
    const temporizador = setTimeout(() => terminar(MENSAJE_VENCIDA), Math.max(restante, 0));
    return () => clearTimeout(temporizador);
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
      clienteConsultas.clear();
      guardarSesion(credencial);
      tokenActual.current = credencial.token;
      setAviso(null);
      setSesion(credencial);
    },
    [clienteConsultas],
  );

  const cerrarSesion = useCallback(
    async (mensaje) => {
      try {
        await pedir('/api/sesion/cerrar', { metodo: 'POST' });
      } catch {
        // Aunque el servidor no responda, la sesión se olvida en este navegador.
      }
      terminar(mensaje);
    },
    [terminar],
  );

  const usuario = consultaUsuario.data ?? null;
  const valor = useMemo(
    () => ({
      autenticado: Boolean(sesion),
      usuario,
      cargandoUsuario: Boolean(sesion) && consultaUsuario.isPending,
      errorUsuario: consultaUsuario.error,
      reintentarUsuario: consultaUsuario.refetch,
      esAdministrador: usuario?.rol === ROL_ADMINISTRADOR,
      tienePermiso: (codigo) => Boolean(usuario?.permisos?.includes(codigo)),
      iniciarSesion,
      cerrarSesion,
      terminarSesion: terminar,
      aviso,
      limpiarAviso: () => setAviso(null),
    }),
    [sesion, usuario, consultaUsuario.isPending, consultaUsuario.error, consultaUsuario.refetch, iniciarSesion, cerrarSesion, terminar, aviso],
  );

  return <ContextoSesion.Provider value={valor}>{children}</ContextoSesion.Provider>;
}
