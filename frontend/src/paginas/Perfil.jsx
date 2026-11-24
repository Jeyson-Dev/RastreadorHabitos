import { useRef, useState } from 'react';
import { LogOut } from 'lucide-react';
import { cambiarContrasena } from '../api/acceso';
import { mensajeDeError } from '../api/cliente';
import Aviso from '../componentes/Aviso';
import Boton from '../componentes/Boton';
import CampoContrasena from '../componentes/CampoContrasena';
import Tarjeta from '../componentes/Tarjeta';
import { ROL_ADMINISTRADOR, useSesion } from '../sesion/contextoSesion';
import { validarContrasena } from '../utilidades/contrasena';
import { enfocarPrimerInvalido } from '../utilidades/formularios';

const NOMBRES_PERMISO = { LIMITE_HABITOS_AMPLIADO: 'Límite de hábitos ampliado (20)' };

// Mi cuenta: datos de la sesión y cambio de contraseña. Cambiarla cierra todas las sesiones en el
// servidor, así que aquí solo se olvida la sesión y se vuelve al inicio con el aviso.
export default function Perfil() {
  const { usuario, cerrarSesion, olvidarSesion } = useSesion();
  const [contrasenaActual, setContrasenaActual] = useState('');
  const [contrasenaNueva, setContrasenaNueva] = useState('');
  const [errores, setErrores] = useState({});
  const [error, setError] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const formulario = useRef(null);

  async function alCambiar(evento) {
    evento.preventDefault();
    setError(null);
    const encontrados = {};
    if (!contrasenaActual) encontrados.contrasenaActual = 'Escribe tu contraseña actual.';
    const errorNueva = validarContrasena(contrasenaNueva);
    if (errorNueva) encontrados.contrasenaNueva = errorNueva;
    else if (contrasenaNueva === contrasenaActual) encontrados.contrasenaNueva = 'La contraseña nueva debe ser distinta de la actual.';
    setErrores(encontrados);
    if (Object.keys(encontrados).length > 0) {
      enfocarPrimerInvalido(formulario.current);
      return;
    }

    setEnviando(true);
    try {
      const respuesta = await cambiarContrasena({ contrasenaActual, contrasenaNueva });
      olvidarSesion(respuesta?.mensaje ?? 'Contraseña actualizada. Inicia sesión de nuevo con la contraseña nueva.');
    } catch (e) {
      setError(mensajeDeError(e));
      setEnviando(false);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Tarjeta aria-labelledby="titulo-datos">
        <h2 id="titulo-datos" className="mb-4 font-semibold">
          Tus datos
        </h2>
        <dl className="grid gap-4 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-suave">Nombre</dt>
            <dd className="font-medium">{usuario?.nombreCompleto}</dd>
          </div>
          <div>
            <dt className="text-suave">Correo</dt>
            <dd className="font-medium break-all">{usuario?.email}</dd>
          </div>
          <div>
            <dt className="text-suave">Rol</dt>
            <dd className="font-medium">{usuario?.rol === ROL_ADMINISTRADOR ? 'Administrador' : 'Estándar'}</dd>
          </div>
          <div>
            <dt className="text-suave">Permisos</dt>
            <dd className="font-medium">
              {usuario?.permisos?.length ? usuario.permisos.map((p) => NOMBRES_PERMISO[p] ?? p).join(', ') : 'Ninguno'}
            </dd>
          </div>
        </dl>
      </Tarjeta>

      <Tarjeta aria-labelledby="titulo-contrasena">
        <h2 id="titulo-contrasena" className="font-semibold">
          Cambiar contraseña
        </h2>
        <p className="mb-4 text-sm text-suave">Al cambiarla se cierran todas tus sesiones y tendrás que entrar de nuevo.</p>
        <form ref={formulario} onSubmit={alCambiar} noValidate className="flex max-w-sm flex-col gap-4">
          {error && <Aviso tipo="error">{error}</Aviso>}
          <CampoContrasena
            etiqueta="Contraseña actual"
            autoComplete="current-password"
            value={contrasenaActual}
            onChange={(e) => setContrasenaActual(e.target.value)}
            error={errores.contrasenaActual}
          />
          <CampoContrasena
            etiqueta="Contraseña nueva"
            autoComplete="new-password"
            value={contrasenaNueva}
            onChange={(e) => setContrasenaNueva(e.target.value)}
            error={errores.contrasenaNueva}
            conRequisitos
          />
          <Boton type="submit" cargando={enviando} className="self-start">
            Cambiar contraseña
          </Boton>
        </form>
      </Tarjeta>

      <div>
        <Boton variante="secundario" onClick={() => cerrarSesion()}>
          <LogOut className="size-4" aria-hidden="true" />
          Cerrar sesión
        </Boton>
      </div>
    </div>
  );
}
