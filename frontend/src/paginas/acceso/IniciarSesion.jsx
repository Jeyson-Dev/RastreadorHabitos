import { useState } from 'react';
import { Link } from 'react-router';
import { reenviarActivacion } from '../../api/acceso';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import CampoContrasena from '../../componentes/CampoContrasena';
import { useSesion } from '../../sesion/contextoSesion';
import { pareceCorreo } from '../../utilidades/contrasena';
import EncabezadoAcceso from './EncabezadoAcceso';

// Inicio de sesión. Un 403 (cuenta sin activar o desactivada) ofrece reenviar la activación: la API
// responde siempre lo mismo, así que no revela si el correo existe.
export default function IniciarSesion() {
  const { iniciarSesion, aviso, limpiarAviso } = useSesion();
  const [email, setEmail] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState(null);
  const [puedeReenviar, setPuedeReenviar] = useState(false);
  const [reenvio, setReenvio] = useState({ estado: 'inactivo', mensaje: null });
  const [enviando, setEnviando] = useState(false);

  async function alEnviar(evento) {
    evento.preventDefault();
    setError(null);
    setPuedeReenviar(false);
    setReenvio({ estado: 'inactivo', mensaje: null });
    limpiarAviso();
    if (!pareceCorreo(email) || !contrasena) {
      setError('Escribe tu correo y tu contraseña.');
      return;
    }
    setEnviando(true);
    try {
      await iniciarSesion(email.trim(), contrasena);
    } catch (e) {
      setError(mensajeDeError(e));
      setPuedeReenviar(e?.estado === 403);
      setEnviando(false);
    }
  }

  async function alReenviar() {
    setReenvio({ estado: 'enviando', mensaje: null });
    try {
      const respuesta = await reenviarActivacion(email.trim());
      setReenvio({ estado: 'listo', mensaje: respuesta?.mensaje ?? 'Si corresponde, te enviamos un nuevo enlace.' });
    } catch (e) {
      setReenvio({ estado: 'error', mensaje: mensajeDeError(e) });
    }
  }

  return (
    <>
      <EncabezadoAcceso titulo="Inicia sesión">Registra tus hábitos y avanza en tus metas.</EncabezadoAcceso>

      <form onSubmit={alEnviar} noValidate className="flex flex-col gap-4">
        {aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>}
        {error && (
          <Aviso tipo="error">
            {error}
            {puedeReenviar && reenvio.estado !== 'listo' && (
              <button
                type="button"
                onClick={alReenviar}
                disabled={reenvio.estado === 'enviando'}
                className="mt-1 block font-medium text-acento underline-offset-2 hover:underline disabled:opacity-60"
              >
                {reenvio.estado === 'enviando' ? 'Enviando…' : 'Reenviar el enlace de activación'}
              </button>
            )}
          </Aviso>
        )}
        {reenvio.mensaje && <Aviso tipo={reenvio.estado === 'error' ? 'error' : 'exito'}>{reenvio.mensaje}</Aviso>}

        <Campo
          etiqueta="Correo"
          type="email"
          autoComplete="email"
          inputMode="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <CampoContrasena
          autoComplete="current-password"
          value={contrasena}
          onChange={(e) => setContrasena(e.target.value)}
          required
          accesorio={
            <Link to="/recuperar" state={{ email }} className="text-xs font-medium text-acento hover:underline">
              ¿Olvidaste tu contraseña?
            </Link>
          }
        />
        <Boton type="submit" cargando={enviando} tamano="lg" className="mt-2 w-full">
          Iniciar sesión
        </Boton>
      </form>

      <p className="mt-8 text-center text-sm text-suave">
        ¿No tienes cuenta?{' '}
        <Link to="/registro" className="font-medium text-acento hover:underline">
          Crear cuenta
        </Link>
      </p>
    </>
  );
}
