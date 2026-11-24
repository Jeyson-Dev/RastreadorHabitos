import { useState } from 'react';
import { MailCheck } from 'lucide-react';
import { Link } from 'react-router';
import { registrarCuenta, reenviarActivacion } from '../../api/acceso';
import { mensajeDeError } from '../../api/cliente';
import Aviso from '../../componentes/Aviso';
import Boton from '../../componentes/Boton';
import Campo from '../../componentes/Campo';
import CampoContrasena from '../../componentes/CampoContrasena';
import { pareceCorreo, validarContrasena } from '../../utilidades/contrasena';
import EncabezadoAcceso from './EncabezadoAcceso';

const LONGITUD_MAXIMA_NOMBRE = 150;

function validar({ nombreCompleto, email, contrasena }) {
  const errores = {};
  if (!nombreCompleto.trim()) errores.nombreCompleto = 'Escribe tu nombre.';
  else if (nombreCompleto.trim().length > LONGITUD_MAXIMA_NOMBRE) errores.nombreCompleto = 'Tu nombre puede tener hasta 150 caracteres.';
  if (!pareceCorreo(email)) errores.email = 'Escribe un correo válido, por ejemplo ana@correo.com.';
  const errorContrasena = validarContrasena(contrasena);
  if (errorContrasena) errores.contrasena = errorContrasena;
  return errores;
}

// Registro: al crearse, la cuenta queda pendiente hasta abrir el enlace del correo.
export default function Registro() {
  const [datos, setDatos] = useState({ nombreCompleto: '', email: '', contrasena: '' });
  const [errores, setErrores] = useState({});
  const [error, setError] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const [registrado, setRegistrado] = useState(null);
  const [reenvio, setReenvio] = useState(null);

  const cambiar = (campo) => (e) => setDatos((d) => ({ ...d, [campo]: e.target.value }));

  async function alEnviar(evento) {
    evento.preventDefault();
    setError(null);
    const encontrados = validar(datos);
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) return;

    setEnviando(true);
    try {
      await registrarCuenta({ ...datos, nombreCompleto: datos.nombreCompleto.trim(), email: datos.email.trim() });
      setRegistrado(datos.email.trim());
    } catch (e) {
      setError(mensajeDeError(e));
    } finally {
      setEnviando(false);
    }
  }

  async function alReenviar() {
    try {
      const respuesta = await reenviarActivacion(registrado);
      setReenvio({ tipo: 'exito', texto: respuesta?.mensaje ?? 'Te enviamos un nuevo enlace.' });
    } catch (e) {
      setReenvio({ tipo: 'error', texto: mensajeDeError(e) });
    }
  }

  if (registrado) {
    return (
      <div className="flex flex-col gap-4">
        <MailCheck className="size-10 text-acento" aria-hidden="true" />
        <EncabezadoAcceso titulo="Revisa tu correo">
          Enviamos un enlace de activación a <strong className="text-texto">{registrado}</strong>. Ábrelo para activar tu
          cuenta; vence en 24 horas y sirve una sola vez.
        </EncabezadoAcceso>
        {reenvio && <Aviso tipo={reenvio.tipo}>{reenvio.texto}</Aviso>}
        <Boton variante="secundario" onClick={alReenviar}>
          Reenviar el enlace
        </Boton>
        <Link to="/iniciar-sesion" className="text-center text-sm font-medium text-acento hover:underline">
          Ir a iniciar sesión
        </Link>
      </div>
    );
  }

  return (
    <>
      <EncabezadoAcceso titulo="Crea tu cuenta">Empieza a registrar tus hábitos hoy.</EncabezadoAcceso>

      <form onSubmit={alEnviar} noValidate className="flex flex-col gap-4">
        {error && <Aviso tipo="error">{error}</Aviso>}
        <Campo
          etiqueta="Nombre completo"
          autoComplete="name"
          value={datos.nombreCompleto}
          onChange={cambiar('nombreCompleto')}
          error={errores.nombreCompleto}
          maxLength={LONGITUD_MAXIMA_NOMBRE}
          required
        />
        <Campo
          etiqueta="Correo"
          type="email"
          autoComplete="email"
          inputMode="email"
          value={datos.email}
          onChange={cambiar('email')}
          error={errores.email}
          required
        />
        <CampoContrasena
          autoComplete="new-password"
          value={datos.contrasena}
          onChange={cambiar('contrasena')}
          error={errores.contrasena}
          conRequisitos
          required
        />
        <Boton type="submit" cargando={enviando} tamano="lg" className="mt-2 w-full">
          Crear cuenta
        </Boton>
      </form>

      <p className="mt-8 text-center text-sm text-suave">
        ¿Ya tienes cuenta?{' '}
        <Link to="/iniciar-sesion" className="font-medium text-acento hover:underline">
          Inicia sesión
        </Link>
      </p>
    </>
  );
}
