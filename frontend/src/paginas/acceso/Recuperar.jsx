import { useEffect, useRef, useState } from 'react';
import { CircleCheck } from 'lucide-react';
import { Link, useLocation } from 'react-router';
import { restablecerContrasena, solicitarCodigoRecuperacion } from '../../api/acceso';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import CampoContrasena from '../../componentes/CampoContrasena';
import { pareceCorreo, validarContrasena } from '../../utilidades/contrasena';
import { enfocarPrimerInvalido } from '../../utilidades/formularios';
import EncabezadoAcceso from './EncabezadoAcceso';

// Recuperación en una pantalla y dos pasos: pedir el código por correo y escribirlo junto con la
// contraseña nueva. La API responde lo mismo exista o no el correo.
export default function Recuperar() {
  const { state } = useLocation();
  const [paso, setPaso] = useState(1);
  const [email, setEmail] = useState(typeof state?.email === 'string' ? state.email : '');
  const [codigo, setCodigo] = useState('');
  const [contrasenaNueva, setContrasenaNueva] = useState('');
  const [errores, setErrores] = useState({});
  const [aviso, setAviso] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const [listo, setListo] = useState(null);
  const titulo = useRef(null);
  const formulario = useRef(null);
  const primerPaso = useRef(true);

  // Al cambiar de paso, el foco va al título nuevo para que se anuncie (no en la primera carga).
  useEffect(() => {
    if (primerPaso.current) {
      primerPaso.current = false;
      return;
    }
    titulo.current?.focus();
  }, [paso]);

  async function pedirCodigo(evento) {
    evento?.preventDefault();
    if (enviando) return;
    setAviso(null);
    if (!pareceCorreo(email)) {
      setErrores({ email: 'Escribe un correo válido, por ejemplo ana@correo.com.' });
      enfocarPrimerInvalido(formulario.current);
      return;
    }
    setErrores({});
    setEnviando(true);
    try {
      const respuesta = await solicitarCodigoRecuperacion(email.trim());
      setAviso({ tipo: 'exito', texto: respuesta?.mensaje ?? 'Si el correo está registrado, recibirás un código.' });
      setPaso(2);
    } catch (e) {
      setAviso({ tipo: 'error', texto: mensajeDeError(e) });
    } finally {
      setEnviando(false);
    }
  }

  async function restablecer(evento) {
    evento.preventDefault();
    setAviso(null);
    const encontrados = {};
    if (!codigo.trim()) encontrados.codigo = 'Escribe el código que te llegó por correo.';
    const errorContrasena = validarContrasena(contrasenaNueva);
    if (errorContrasena) encontrados.contrasenaNueva = errorContrasena;
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) {
      enfocarPrimerInvalido(formulario.current);
      return;
    }

    setEnviando(true);
    try {
      const respuesta = await restablecerContrasena({ email: email.trim(), codigo: codigo.trim(), contrasenaNueva });
      setListo(respuesta?.mensaje ?? 'Contraseña restablecida. Inicia sesión con la contraseña nueva.');
    } catch (e) {
      setAviso({ tipo: 'error', texto: mensajeDeError(e) });
    } finally {
      setEnviando(false);
    }
  }

  if (listo) {
    return (
      <div className="flex flex-col gap-4">
        <CircleCheck className="size-10 text-acento" aria-hidden="true" />
        <EncabezadoAcceso titulo="Contraseña restablecida">{listo}</EncabezadoAcceso>
        <Link
          to="/iniciar-sesion"
          className="inline-flex h-11 items-center justify-center rounded-lg bg-acento px-5 font-medium text-sobre-acento hover:bg-acento-hover"
        >
          Iniciar sesión
        </Link>
      </div>
    );
  }

  return (
    <>
      <p className="numeros mb-2 text-xs font-semibold tracking-wide text-suave uppercase">Paso {paso} de 2</p>
      {paso === 1 ? (
        <>
          <EncabezadoAcceso ref={titulo} titulo="Recupera tu contraseña">Te enviaremos un código a tu correo para crear una nueva.</EncabezadoAcceso>
          <form ref={formulario} onSubmit={pedirCodigo} noValidate className="flex flex-col gap-4">
            {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
            <Campo
              etiqueta="Correo"
              type="email"
              autoComplete="email"
              inputMode="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              error={errores.email}
              required
            />
            <Boton type="submit" cargando={enviando} tamano="lg" className="mt-2 w-full">
              Enviar código
            </Boton>
          </form>
        </>
      ) : (
        <>
          <EncabezadoAcceso ref={titulo} titulo="Escribe el código">
            Si <strong className="text-texto">{email.trim()}</strong> está registrado, te llegó un código de un solo uso que vence en 30
            minutos.
          </EncabezadoAcceso>
          <form ref={formulario} onSubmit={restablecer} noValidate className="flex flex-col gap-4">
            {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
            <Campo
              etiqueta="Código"
              autoComplete="one-time-code"
              autoCapitalize="characters"
              placeholder="K7Q2-M9XP"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value)}
              error={errores.codigo}
              required
            />
            <CampoContrasena
              etiqueta="Contraseña nueva"
              autoComplete="new-password"
              value={contrasenaNueva}
              onChange={(e) => setContrasenaNueva(e.target.value)}
              error={errores.contrasenaNueva}
              conRequisitos
              required
            />
            <Boton type="submit" cargando={enviando} tamano="lg" className="mt-2 w-full">
              Cambiar contraseña
            </Boton>
            <div className="flex justify-between text-sm">
              <button
                type="button"
                onClick={() => pedirCodigo()}
                disabled={enviando}
                className="font-medium text-acento hover:underline disabled:opacity-60"
              >
                {enviando ? 'Enviando…' : 'Reenviar código'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setPaso(1);
                  setAviso(null);
                  setErrores({});
                  setCodigo('');
                  setContrasenaNueva('');
                }}
                className="text-suave hover:text-texto"
              >
                Cambiar correo
              </button>
            </div>
          </form>
        </>
      )}

      <p className="mt-8 text-center text-sm text-suave">
        <Link to="/iniciar-sesion" className="font-medium text-acento hover:underline">
          Volver a iniciar sesión
        </Link>
      </p>
    </>
  );
}
