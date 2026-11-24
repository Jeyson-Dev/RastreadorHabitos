import { CalendarCheck, ListChecks, Plus } from 'lucide-react';
import { Link } from 'react-router';
import Cargando from '../../componentes/Cargando';
import EstadoError from '../../componentes/EstadoError';
import EstadoVacio from '../../componentes/EstadoVacio';
import Tarjeta from '../../componentes/Tarjeta';
import { diaLocalDe, fechaLarga } from '../../utilidades/fechas';
import BotonesRegistro from './BotonesRegistro';
import { useHabitos } from './consultas';
import Racha from './Racha';
import TarjetaClima from './TarjetaClima';

const enlaceCrear = (
  <Link
    to="/habitos?nuevo=1"
    className="inline-flex h-10 items-center gap-2 rounded-lg bg-acento px-4 text-sm font-medium text-sobre-acento hover:bg-acento-hover"
  >
    <Plus className="size-4" aria-hidden="true" />
    Crear un hábito
  </Link>
);

// Hoy: progreso del día y clima arriba; debajo, los hábitos programados para hoy con su registro.
// El estado de cada hábito sale de GET /api/habitos, así que funciona aunque el clima no esté.
export default function Hoy() {
  const habitos = useHabitos();

  if (habitos.isPending) return <Cargando texto="Cargando tu día…" />;
  if (habitos.isError) return <EstadoError error={habitos.error} alReintentar={habitos.refetch} />;

  const deHoy = habitos.data.filter((h) => h.programadoHoy);
  const otros = habitos.data.filter((h) => !h.programadoHoy);
  const cumplidos = deHoy.filter((h) => h.cumplidoHoy === true).length;
  const porcentaje = deHoy.length ? Math.round((cumplidos / deHoy.length) * 100) : 0;
  const pendientes = deHoy.filter((h) => h.cumplidoHoy === null || h.cumplidoHoy === undefined).length;

  return (
    <div className="flex flex-col gap-6">
      <h2 className="text-sm font-medium text-suave capitalize">{fechaLarga(diaLocalDe(new Date()))}</h2>

      <div className="grid gap-4 md:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <Tarjeta aria-labelledby="titulo-progreso" className="flex flex-col justify-center gap-3">
          <h3 id="titulo-progreso" className="sr-only">
            Progreso de hoy
          </h3>
          <p className="numeros text-3xl font-semibold">
            {cumplidos} <span className="text-base font-normal text-suave">de {deHoy.length} cumplidos</span>
          </p>
          <div
            role="progressbar"
            aria-label="Hábitos cumplidos hoy"
            aria-valuemin={0}
            aria-valuemax={deHoy.length}
            aria-valuenow={cumplidos}
            className="h-2.5 overflow-hidden rounded-full bg-superficie-suave"
          >
            <div className="h-full rounded-full bg-acento transition-[width] duration-500" style={{ width: `${porcentaje}%` }} />
          </div>
          <p className="text-sm text-suave">
            {deHoy.length === 0
              ? 'Hoy no tienes hábitos programados.'
              : pendientes === 0
                ? '¡Registraste todo lo de hoy!'
                : `Te ${pendientes === 1 ? 'falta registrar 1 hábito' : `faltan registrar ${pendientes} hábitos`}.`}
          </p>
        </Tarjeta>
        <TarjetaClima />
      </div>

      {habitos.data.length === 0 ? (
        <EstadoVacio icono={ListChecks} titulo="Aún no tienes hábitos" accion={enlaceCrear}>
          Crea tu primer hábito y elige qué días de la semana lo harás.
        </EstadoVacio>
      ) : deHoy.length === 0 ? (
        <EstadoVacio icono={CalendarCheck} titulo="Hoy es un día libre">
          Ninguno de tus hábitos está programado para hoy.
        </EstadoVacio>
      ) : (
        <section aria-labelledby="titulo-lista-hoy" className="flex flex-col gap-3">
          <h3 id="titulo-lista-hoy" className="font-semibold">
            Para hoy
          </h3>
          <ul className="flex flex-col divide-y divide-borde overflow-hidden rounded-xl border border-borde bg-superficie">
            {deHoy.map((habito) => (
              <li key={habito.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3.5 sm:px-5">
                <div className="flex min-w-0 items-center gap-3">
                  <span
                    className={`size-2.5 shrink-0 rounded-full ${
                      habito.cumplidoHoy === true ? 'bg-acento' : habito.cumplidoHoy === false ? 'bg-peligro' : 'ring-2 ring-borde ring-inset'
                    }`}
                    aria-hidden="true"
                  />
                  <span className="truncate font-medium">{habito.nombre}</span>
                  <Racha dias={habito.rachaActual} />
                </div>
                <BotonesRegistro habito={habito} />
              </li>
            ))}
          </ul>
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
