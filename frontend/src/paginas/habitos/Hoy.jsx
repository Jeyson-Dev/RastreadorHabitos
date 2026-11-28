import { CalendarCheck, ListChecks, Plus } from 'lucide-react';
import { m } from 'motion/react';
import { Link } from 'react-router';
import AnilloProgreso from '../../componentes/AnilloProgreso';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import IconoHabito from '../../componentes/IconoHabito';
import Tarjeta from '../../componentes/Tarjeta';
import { useSesion } from '../../sesion/contextoSesion';
import { capitalizar, diaLocalDe, fechaLarga } from '../../utilidades/fechas';
import { useEscalonado } from '../../utilidades/movimiento';
import BotonesRegistro from './BotonesRegistro';
import { useHabitos } from './consultas';
import Racha from './Racha';
import TarjetaClima from './TarjetaClima';

const enlaceCrear = (
  <Link
    to="/habitos?nuevo=1"
    className="inline-flex h-10 items-center gap-2 rounded-xl bg-acento px-4 text-sm font-medium text-sobre-acento shadow-acento transition-transform hover:bg-acento-hover active:scale-[0.97]"
  >
    <Plus className="size-4" aria-hidden="true" />
    Crear un hábito
  </Link>
);

function saludo(hora) {
  if (hora < 12) return 'Buenos días';
  if (hora < 19) return 'Buenas tardes';
  return 'Buenas noches';
}

function mensajeDelDia(total, cumplidos, pendientes) {
  if (total === 0) return 'Hoy no tienes hábitos programados.';
  if (pendientes === 0) return cumplidos === total ? '¡Día completo! Cumpliste todo lo de hoy.' : '¡Registraste todo lo de hoy!';
  if (cumplidos === 0) return `Empieza por uno: te ${pendientes === 1 ? 'falta registrar 1 hábito' : `faltan registrar ${pendientes} hábitos`}.`;
  return `¡Vas bien! Te ${pendientes === 1 ? 'falta registrar 1 hábito' : `faltan registrar ${pendientes} hábitos`}.`;
}

// Hoy: saludo, anillo con el progreso del día y clima arriba; debajo, una tarjeta por hábito
// programado con su registro. El estado sale de GET /api/habitos, así que funciona sin el clima.
export default function Hoy() {
  const habitos = useHabitos();
  const { usuario } = useSesion();
  const { contenedor, elemento } = useEscalonado();

  if (habitos.isPending) return <Cargando texto="Cargando tu día…" />;
  if (habitos.isError) return <EstadoError error={habitos.error} alReintentar={habitos.refetch} />;

  const activos = habitos.data.filter((h) => !h.archivado);
  const deHoy = activos.filter((h) => h.programadoHoy);
  const otros = activos.filter((h) => !h.programadoHoy);
  const cumplidos = deHoy.filter((h) => h.cumplidoHoy === true).length;
  const pendientes = deHoy.filter((h) => h.cumplidoHoy === null || h.cumplidoHoy === undefined).length;
  const nombre = usuario?.nombreCompleto?.trim().split(/\s+/)[0];
  const ahora = new Date();

  return (
    <div className="flex flex-col gap-7">
      <div className="flex flex-col gap-1">
        <h2 className="text-3xl font-semibold sm:text-4xl">
          {saludo(ahora.getHours())}
          {nombre ? `, ${nombre}` : ''}
        </h2>
        <p className="text-sm text-suave">{capitalizar(fechaLarga(diaLocalDe(ahora)))}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <Tarjeta aria-labelledby="titulo-progreso" className="flex items-center gap-5 sm:gap-7">
          <h3 id="titulo-progreso" className="sr-only">
            Progreso de hoy
          </h3>
          {deHoy.length > 0 && (
            <AnilloProgreso valor={cumplidos} maximo={deHoy.length} etiqueta="Hábitos cumplidos hoy">
              <span className="titulo numeros text-3xl font-semibold">
                {cumplidos}
                <span className="text-lg text-suave">/{deHoy.length}</span>
              </span>
            </AnilloProgreso>
          )}
          <div className="flex min-w-0 flex-col gap-1.5">
            <p className="titulo numeros text-2xl font-semibold">
              {cumplidos} <span className="text-base font-normal text-suave">de {deHoy.length} cumplidos</span>
            </p>
            <p className="text-sm text-suave">{mensajeDelDia(deHoy.length, cumplidos, pendientes)}</p>
          </div>
        </Tarjeta>
        <TarjetaClima />
      </div>

      {activos.length === 0 ? (
        <EstadoVacio icono={ListChecks} titulo="Aún no tienes hábitos" accion={enlaceCrear}>
          Crea tu primer hábito y elige qué días de la semana lo harás.
        </EstadoVacio>
      ) : deHoy.length === 0 ? (
        <EstadoVacio icono={CalendarCheck} titulo="Hoy es un día libre">
          Ninguno de tus hábitos está programado para hoy.
        </EstadoVacio>
      ) : (
        <section aria-labelledby="titulo-lista-hoy" className="flex flex-col gap-3">
          <h3 id="titulo-lista-hoy" className="titulo text-xl font-semibold">
            Para hoy
          </h3>
          <m.ul {...contenedor} className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {deHoy.map((habito) => (
              <m.li
                key={habito.id}
                variants={elemento.variants}
                className={`vidrio flex flex-col gap-4 rounded-[1.25rem] p-4 transition-shadow ${
                  habito.cumplidoHoy === true ? 'ring-2 ring-exito/40' : habito.cumplidoHoy === false ? 'ring-1 ring-peligro/30' : ''
                }`}
              >
                <div className="flex items-start gap-3">
                  <IconoHabito habito={habito} />
                  <div className="flex min-w-0 flex-1 flex-col">
                    <span className="truncate font-semibold">{habito.nombre}</span>
                    {habito.descripcion && <span className="truncate text-xs text-suave">{habito.descripcion}</span>}
                  </div>
                  <Racha dias={habito.rachaActual} />
                </div>
                <BotonesRegistro habito={habito} />
              </m.li>
            ))}
          </m.ul>
        </section>
      )}

      {otros.length > 0 && deHoy.length > 0 && (
        <p className="text-sm text-suave">
          Hoy no toca: {otros.map((h) => h.nombre).join(', ')}.{' '}
          <Link to="/habitos" className="font-medium text-acento hover:underline">
            Ver todos
          </Link>
        </p>
      )}
    </div>
  );
}
