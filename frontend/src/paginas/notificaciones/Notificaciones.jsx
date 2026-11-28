import { useRef, useState } from 'react';
import { BellOff, CircleCheck, CircleX, KeyRound, Mail, Target } from 'lucide-react';
import { Link } from 'react-router';
import { mensajeDeError } from '../../api/cliente';
import { TIPOS_NOTIFICACION } from '../../api/notificaciones';
import Boton from '../../componentes/Boton';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import { useSesion } from '../../sesion/contextoSesion';
import { fechaHoraLocal, haceCuanto } from '../../utilidades/tiempo';
import { useActualizarPreferencias, useBandeja, useMarcarLeida, usePreferencias } from './consultas';

const PRESENTACION = {
  [TIPOS_NOTIFICACION.SolicitudPermisoCreada]: { icono: KeyRound, clase: 'text-info', titulo: 'Solicitud de permiso' },
  [TIPOS_NOTIFICACION.SolicitudPermisoAprobada]: { icono: CircleCheck, clase: 'text-acento', titulo: 'Permiso aprobado' },
  [TIPOS_NOTIFICACION.SolicitudPermisoRechazada]: { icono: CircleX, clase: 'text-peligro', titulo: 'Permiso rechazado' },
  [TIPOS_NOTIFICACION.MetaCambioEstado]: { icono: Target, clase: 'text-racha', titulo: 'Tu meta cambió de estado' },
};

function destino(tipo, esAdministrador) {
  if (tipo === TIPOS_NOTIFICACION.MetaCambioEstado) return { a: '/metas', texto: 'Ver metas' };
  if (tipo === TIPOS_NOTIFICACION.SolicitudPermisoCreada && esAdministrador) return { a: '/admin/solicitudes', texto: 'Revisar solicitudes' };
  if (tipo === TIPOS_NOTIFICACION.SolicitudPermisoAprobada) return { a: '/habitos', texto: 'Ir a hábitos' };
  return null;
}

// Bandeja: todas o solo las no leídas, marcar como leída y la preferencia de recibirlas por correo.
export default function Notificaciones() {
  const [soloNoLeidas, setSoloNoLeidas] = useState(false);
  const bandeja = useBandeja(soloNoLeidas);
  const marcar = useMarcarLeida();
  const { esAdministrador } = useSesion();
  const tituloBandeja = useRef(null);

  let contenido;
  if (bandeja.isPending) contenido = <Cargando texto="Cargando tus notificaciones…" />;
  else if (bandeja.isError) contenido = <EstadoError error={bandeja.error} alReintentar={bandeja.refetch} />;
  else if (bandeja.data.notificaciones.length === 0) {
    contenido = (
      <EstadoVacio icono={BellOff} titulo={soloNoLeidas ? 'No tienes notificaciones sin leer' : 'Aún no tienes notificaciones'}>
        Te avisaremos aquí cuando una meta cambie de estado o cuando se resuelva una solicitud de permiso.
      </EstadoVacio>
    );
  } else {
    contenido = (
      <ul className="flex flex-col divide-y divide-borde overflow-hidden vidrio rounded-[1.25rem]">
        {bandeja.data.notificaciones.map((n) => {
          const { icono: Icono, clase, titulo } = PRESENTACION[n.tipo] ?? { icono: Mail, clase: 'text-suave', titulo: 'Aviso' };
          const enlace = destino(n.tipo, esAdministrador);
          const marcando = marcar.isPending && marcar.variables === n.id;
          return (
            <li key={n.id} className={`flex gap-3 px-4 py-4 sm:px-5 ${n.leida ? '' : 'bg-acento-suave/40'}`}>
              <Icono className={`mt-0.5 size-5 shrink-0 ${clase}`} aria-hidden="true" />
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <p className="flex flex-wrap items-center gap-2 text-sm font-medium">
                  {titulo}
                  {!n.leida && (
                    <span className="rounded-full bg-acento px-2 py-0.5 text-[11px] font-semibold text-sobre-acento">Nueva</span>
                  )}
                </p>
                <p className="text-sm text-suave">{n.mensaje}</p>
                <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-suave">
                  <time dateTime={n.fechaUtc} title={fechaHoraLocal(n.fechaUtc)}>
                    {haceCuanto(n.fechaUtc)}
                  </time>
                  {enlace && (
                    <Link to={enlace.a} className="font-medium text-acento hover:underline">
                      {enlace.texto}
                    </Link>
                  )}
                  {!n.leida && (
                    <button
                      type="button"
                      onClick={() => marcar.mutate(n.id, { onSuccess: () => soloNoLeidas && tituloBandeja.current?.focus() })}
                      disabled={marcando}
                      className="font-medium text-texto underline-offset-2 hover:underline disabled:opacity-60"
                      aria-label={`Marcar como leída: ${titulo}`}
                    >
                      {marcando ? 'Marcando…' : 'Marcar como leída'}
                    </button>
                  )}
                </div>
              </div>
            </li>
          );
        })}
      </ul>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PreferenciaCorreo />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 ref={tituloBandeja} tabIndex={-1} className="font-semibold outline-none">
          Bandeja{' '}
          {bandeja.data?.noLeidas > 0 && <span className="numeros text-sm font-normal text-suave">· {bandeja.data.noLeidas} sin leer</span>}
        </h2>
        <div role="group" aria-label="Filtrar notificaciones" className="inline-flex rounded-lg border border-borde p-0.5">
          {[
            [false, 'Todas'],
            [true, 'No leídas'],
          ].map(([valor, texto]) => (
            <button
              key={texto}
              type="button"
              aria-pressed={soloNoLeidas === valor}
              onClick={() => setSoloNoLeidas(valor)}
              className={`rounded-md px-3 py-1.5 text-sm font-medium ${soloNoLeidas === valor ? 'bg-superficie-suave text-texto' : 'text-suave hover:text-texto'}`}
            >
              {texto}
            </button>
          ))}
        </div>
      </div>
      {marcar.isError && (
        <p role="alert" className="text-sm font-medium text-peligro">
          {mensajeDeError(marcar.error)}
        </p>
      )}
      {contenido}
    </div>
  );
}

// Interruptor accesible (role="switch") para recibir también cada notificación por correo.
function PreferenciaCorreo() {
  const preferencias = usePreferencias();
  const actualizar = useActualizarPreferencias();
  const activo = actualizar.isPending ? actualizar.variables : preferencias.data?.correoActivo;

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 vidrio rounded-[1.25rem] p-4 sm:p-5">
      <div className="flex min-w-0 flex-col gap-0.5">
        <p id="titulo-correo" className="text-sm font-medium">
          Recibir también por correo
        </p>
        <p id="ayuda-correo" className="text-xs text-suave">
          Las notificaciones siempre quedan aquí; esto solo decide si además llegan a tu correo.
        </p>
        {(preferencias.isError || actualizar.isError) && (
          <p role="alert" className="text-xs font-medium text-peligro">
            {mensajeDeError(preferencias.error ?? actualizar.error)}
          </p>
        )}
      </div>
      {preferencias.isPending ? (
        <span className="h-7 w-12 animate-pulse rounded-full bg-superficie-suave" aria-hidden="true" />
      ) : preferencias.isError ? (
        <Boton tamano="sm" variante="secundario" onClick={() => preferencias.refetch()}>
          Reintentar
        </Boton>
      ) : (
        <button
          type="button"
          role="switch"
          aria-checked={Boolean(activo)}
          aria-labelledby="titulo-correo"
          aria-describedby="ayuda-correo"
          disabled={actualizar.isPending}
          onClick={() => actualizar.mutate(!activo)}
          className={`relative inline-flex h-7 w-12 shrink-0 items-center rounded-full border-2 transition-colors focus-visible:ring-2 focus-visible:ring-acento focus-visible:ring-offset-2 focus-visible:outline-none disabled:opacity-60 ${activo ? 'border-acento bg-acento' : 'border-suave bg-superficie-suave'}`}
        >
          <span className={`inline-block size-5 rounded-full bg-white shadow transition-transform ${activo ? 'translate-x-5' : 'translate-x-0.5'}`} />
        </button>
      )}
    </div>
  );
}
