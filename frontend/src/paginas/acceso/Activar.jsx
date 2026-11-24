import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { CircleCheck, CircleX } from 'lucide-react';
import { Link, useSearchParams } from 'react-router';
import { activarCuenta, reenviarActivacion } from '../../api/acceso';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import Cargando from '../../componentes/Cargando';
import { useSesion } from '../../sesion/contextoSesion';
import { pareceCorreo } from '../../utilidades/contrasena';
import EncabezadoAcceso from './EncabezadoAcceso';

// Página a la que lleva el enlace del correo. El token sirve una sola vez: la petición va por la
// caché de consultas (una sola por token, aunque React monte dos veces en desarrollo) y sin señal
// de cancelación, para no repetirla si el componente se desmonta a mitad.
export default function Activar() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token')?.trim() ?? '';
  const { autenticado } = useSesion();

  const activacion = useQuery({
    queryKey: ['activacion', token],
    queryFn: () => activarCuenta(token),
    enabled: token.length > 0,
    retry: false,
    staleTime: Infinity,
    gcTime: Infinity,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  if (token && activacion.isPending) return <Cargando texto="Activando tu cuenta…" />;

  if (token && activacion.isSuccess) {
    return (
      <div className="flex flex-col gap-4">
        <CircleCheck className="size-10 text-acento" aria-hidden="true" />
        <EncabezadoAcceso titulo="¡Cuenta activada!">{activacion.data?.mensaje ?? 'Ya puedes iniciar sesión.'}</EncabezadoAcceso>
        <Link
          to={autenticado ? '/' : '/iniciar-sesion'}
          className="inline-flex h-11 items-center justify-center rounded-lg bg-acento px-5 font-medium text-sobre-acento hover:bg-acento-hover"
        >
          {autenticado ? 'Ir a Hoy' : 'Iniciar sesión'}
        </Link>
      </div>
    );
  }

  const motivo = token ? mensajeDeError(activacion.error) : 'El enlace no trae el código de activación.';
  return (
    <div className="flex flex-col gap-4">
      <CircleX className="size-10 text-peligro" aria-hidden="true" />
      <EncabezadoAcceso titulo="No pudimos activar tu cuenta">{motivo}</EncabezadoAcceso>
      <ReenvioActivacion />
    </div>
  );
}

function ReenvioActivacion() {
  const [email, setEmail] = useState('');
  const [resultado, setResultado] = useState(null);
  const [enviando, setEnviando] = useState(false);

  async function alEnviar(evento) {
    evento.preventDefault();
    if (!pareceCorreo(email)) {
      setResultado({ tipo: 'error', texto: 'Escribe el correo con el que te registraste.' });
      return;
    }
    setEnviando(true);
    try {
      const respuesta = await reenviarActivacion(email.trim());
      setResultado({ tipo: 'exito', texto: respuesta?.mensaje ?? 'Si corresponde, te enviamos un nuevo enlace.' });
    } catch (e) {
      setResultado({ tipo: 'error', texto: mensajeDeError(e) });
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form onSubmit={alEnviar} noValidate className="flex flex-col gap-3 rounded-xl border border-borde bg-superficie p-5">
      <p className="text-sm font-medium">Pide un enlace nuevo</p>
      {resultado && <Aviso tipo={resultado.tipo}>{resultado.texto}</Aviso>}
      <Campo etiqueta="Correo" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
      <Boton type="submit" variante="secundario" cargando={enviando}>
        Reenviar el enlace
      </Boton>
      <Link to="/iniciar-sesion" className="text-center text-sm font-medium text-acento hover:underline">
        Volver a iniciar sesión
      </Link>
    </form>
  );
}
