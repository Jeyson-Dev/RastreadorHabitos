import { useEffect, useState } from 'react';
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

// El token del enlace sirve una sola vez: cada token se envía una única vez por carga de la página y
// su resultado se reutiliza aunque React monte dos veces o se limpie la caché de consultas. Solo si
// no hubo conexión se permite volver a intentarlo.
const activaciones = new Map();

function activarUnaVez(token) {
  if (!activaciones.has(token)) {
    const intento = activarCuenta(token).catch((error) => {
      if (error?.estado === 0) activaciones.delete(token);
      throw error;
    });
    activaciones.set(token, intento);
  }
  return activaciones.get(token);
}

export default function Activar() {
  const [parametros] = useSearchParams();
  const token = parametros.get('token')?.trim() ?? '';
  const { autenticado } = useSesion();
  const [resultado, setResultado] = useState({ estado: token ? 'cargando' : 'sin-token' });

  useEffect(() => {
    if (!token) return undefined;
    let vigente = true;
    activarUnaVez(token).then(
      (datos) => vigente && setResultado({ estado: 'activada', mensaje: datos?.mensaje }),
      (error) => vigente && setResultado({ estado: 'error', mensaje: mensajeDeError(error) }),
    );
    return () => {
      vigente = false;
    };
  }, [token]);

  if (resultado.estado === 'cargando') return <Cargando texto="Activando tu cuenta…" />;

  if (resultado.estado === 'activada') {
    return (
      <div className="flex flex-col gap-4">
        <CircleCheck className="size-10 text-acento" aria-hidden="true" />
        <EncabezadoAcceso titulo="¡Cuenta activada!">{resultado.mensaje ?? 'Ya puedes iniciar sesión.'}</EncabezadoAcceso>
        <Link
          to={autenticado ? '/' : '/iniciar-sesion'}
          className="inline-flex h-11 items-center justify-center rounded-lg bg-acento px-5 font-medium text-sobre-acento hover:bg-acento-hover"
        >
          {autenticado ? 'Ir a Hoy' : 'Iniciar sesión'}
        </Link>
      </div>
    );
  }

  const motivo = resultado.estado === 'sin-token' ? 'El enlace no trae el código de activación.' : resultado.mensaje;
  return (
    <div className="flex flex-col gap-4">
      <CircleX className="size-10 text-peligro" aria-hidden="true" />
      <EncabezadoAcceso titulo="No pudimos activar tu cuenta">{motivo}</EncabezadoAcceso>
      <p className="text-sm text-suave">
        Si ya la activaste antes,{' '}
        <Link to="/iniciar-sesion" className="font-medium text-acento hover:underline">
          inicia sesión
        </Link>
        . Si no, pide un enlace nuevo.
      </p>
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
    if (enviando) return;
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
      <Campo
        etiqueta="Correo"
        type="email"
        autoComplete="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        error={resultado?.tipo === 'error' && !pareceCorreo(email) ? resultado.texto : undefined}
      />
      <Boton type="submit" variante="secundario" cargando={enviando}>
        Reenviar el enlace
      </Boton>
    </form>
  );
}
